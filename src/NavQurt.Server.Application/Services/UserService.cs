using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Core.Entities;

namespace NavQurt.Server.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UserService(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<OperationResult<IReadOnlyList<UserDto>>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            dtos.Add(ToDto(user, roles));
        }

        return OperationResult<IReadOnlyList<UserDto>>.Success(dtos);
    }

    public async Task<OperationResult<UserDto>> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return OperationResult<UserDto>.NotFound("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return OperationResult<UserDto>.Success(ToDto(user, roles));
    }

    public async Task<OperationResult<UserDto>> UpdateUserProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return OperationResult<UserDto>.Invalid("User id is required.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<UserDto>.NotFound("User not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && !string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            var usernameResult = await _userManager.SetUserNameAsync(user, request.UserName);
            if (!usernameResult.Succeeded)
            {
                return OperationResult<UserDto>.Invalid(usernameResult.Errors.Select(e => e.Description).ToArray());
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _userManager.SetEmailAsync(user, request.Email);
            if (!emailResult.Succeeded)
            {
                return OperationResult<UserDto>.Invalid(emailResult.Errors.Select(e => e.Description).ToArray());
            }

            user.EmailConfirmed = false;
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
            if (!phoneResult.Succeeded)
            {
                return OperationResult<UserDto>.Invalid(phoneResult.Errors.Select(e => e.Description).ToArray());
            }
        }

        if (request.FirstName is not null)
        {
            user.FirstName = request.FirstName;
        }

        if (request.LastName is not null)
        {
            user.LastName = request.LastName;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return OperationResult<UserDto>.Invalid(updateResult.Errors.Select(e => e.Description).ToArray());
        }

        var roles = await _userManager.GetRolesAsync(user);
        return OperationResult<UserDto>.Success(ToDto(user, roles));
    }

    public async Task<OperationResult<UserDto>> UpdateUserRolesAsync(UpdateUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return OperationResult<UserDto>.Invalid("User id is required.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<UserDto>.NotFound("User not found.");
        }

        var targetRoles = request.Roles?.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
        foreach (var role in targetRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return OperationResult<UserDto>.Invalid($"Role '{role}' does not exist.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(targetRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return OperationResult<UserDto>.Invalid(removeResult.Errors.Select(e => e.Description).ToArray());
            }
        }

        var rolesToAdd = targetRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return OperationResult<UserDto>.Invalid(addResult.Errors.Select(e => e.Description).ToArray());
            }
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return OperationResult<UserDto>.Success(ToDto(user, updatedRoles));
    }

    public async Task<OperationResult<UserDto>> SetUserActivationAsync(UpdateUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return OperationResult<UserDto>.Invalid("User id is required.");
        }

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<UserDto>.NotFound("User not found.");
        }

        user.IsActive = request.IsActive;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return OperationResult<UserDto>.Invalid(updateResult.Errors.Select(e => e.Description).ToArray());
        }

        var roles = await _userManager.GetRolesAsync(user);
        return OperationResult<UserDto>.Success(ToDto(user, roles));
    }

    public async Task<OperationResult<bool>> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return OperationResult<bool>.NotFound("User not found.");
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            return OperationResult<bool>.Invalid(deleteResult.Errors.Select(e => e.Description).ToArray());
        }

        return OperationResult<bool>.Success(true);
    }

    private static UserDto ToDto(AppUser user, IReadOnlyCollection<string> roles)
    {
        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.Email,
            user.EmailConfirmed,
            user.PhoneNumber,
            user.PhoneNumberConfirmed,
            user.IsActive,
            user.FullName,
            user.CreatedAt,
            roles.ToList());
    }
}
