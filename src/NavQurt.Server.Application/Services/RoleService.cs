using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Core.Constants;
using NavQurt.Server.Core.Entities;

namespace NavQurt.Server.Application.Services;

/// <summary>
///     Provides role management and assignment capabilities on top of ASP.NET Core Identity.
/// </summary>
public sealed class RoleService : IRoleService
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<OperationResult<IReadOnlyList<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name ?? string.Empty, r.NormalizedName ?? string.Empty, 0))
            .ToListAsync(cancellationToken);

        var userCounts = await GetRoleUserCountsAsync(roles.Select(r => r.Name).ToArray(), cancellationToken);

        var result = roles.Select(r => r with { UserCount = userCounts.TryGetValue(r.Name, out var count) ? count : 0 })
            .ToList();

        return OperationResult<IReadOnlyList<RoleDto>>.Success(result);
    }

    public async Task<OperationResult<RoleDto>> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return OperationResult<RoleDto>.Invalid("Role identifier is required.");
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return OperationResult<RoleDto>.NotFound("Role not found.");
        }

        var userCount = await GetUsersInRoleCountAsync(role.Name!, cancellationToken);
        return OperationResult<RoleDto>.Success(ToDto(role, userCount));
    }

    public async Task<OperationResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return OperationResult<RoleDto>.Invalid("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return OperationResult<RoleDto>.Invalid("Role name is required.");
        }

        var normalizedName = _roleManager.NormalizeKey(request.Name);
        var exists = await _roleManager.Roles.AnyAsync(r => r.NormalizedName == normalizedName, cancellationToken);
        if (exists)
        {
            return OperationResult<RoleDto>.Conflict($"Role '{request.Name}' already exists.");
        }

        var role = new AppRole
        {
            Name = request.Name,
            NormalizedName = normalizedName
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            return OperationResult<RoleDto>.Invalid(result.Errors.Select(e => e.Description).ToArray());
        }

        _logger.LogInformation("Created role {RoleName} ({RoleId})", role.Name, role.Id);

        return OperationResult<RoleDto>.Success(ToDto(role, 0));
    }

    public async Task<OperationResult<RoleDto>> UpdateRoleAsync(string roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return OperationResult<RoleDto>.Invalid("Role identifier is required.");
        }

        if (request is null)
        {
            return OperationResult<RoleDto>.Invalid("Request is required.");
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return OperationResult<RoleDto>.NotFound("Role not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return OperationResult<RoleDto>.Invalid("Role name is required.");
        }

        var normalizedName = _roleManager.NormalizeKey(request.Name);
        var exists = await _roleManager.Roles
            .AnyAsync(r => r.Id != roleId && r.NormalizedName == normalizedName, cancellationToken);
        if (exists)
        {
            return OperationResult<RoleDto>.Conflict($"Role '{request.Name}' already exists.");
        }

        role.Name = request.Name;
        role.NormalizedName = normalizedName;

        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            return OperationResult<RoleDto>.Invalid(updateResult.Errors.Select(e => e.Description).ToArray());
        }

        var userCount = await GetUsersInRoleCountAsync(role.Name!, cancellationToken);
        return OperationResult<RoleDto>.Success(ToDto(role, userCount));
    }

    public async Task<OperationResult<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return OperationResult<bool>.Invalid("Role identifier is required.");
        }

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return OperationResult<bool>.NotFound("Role not found.");
        }

        if (string.Equals(role.Name, RoleConstants.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<bool>.Forbidden("The SuperAdmin role cannot be deleted.");
        }

        var userCount = await GetUsersInRoleCountAsync(role.Name!, cancellationToken);
        if (userCount > 0)
        {
            return OperationResult<bool>.Conflict("Role is currently assigned to one or more users.");
        }

        var deleteResult = await _roleManager.DeleteAsync(role);
        if (!deleteResult.Succeeded)
        {
            return OperationResult<bool>.Invalid(deleteResult.Errors.Select(e => e.Description).ToArray());
        }

        _logger.LogInformation("Deleted role {RoleName} ({RoleId})", role.Name, role.Id);

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<RoleAssignmentResponse>> AssignRolesToUserAsync(RoleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        return await ModifyUserRolesAsync(request, addRoles: true, cancellationToken);
    }

    public async Task<OperationResult<RoleAssignmentResponse>> RemoveRolesFromUserAsync(RoleAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        return await ModifyUserRolesAsync(request, addRoles: false, cancellationToken);
    }

    public async Task<OperationResult<RoleAssignmentResponse>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return OperationResult<RoleAssignmentResponse>.Invalid("User id is required.");
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return OperationResult<RoleAssignmentResponse>.NotFound("User not found.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return OperationResult<RoleAssignmentResponse>.Success(new RoleAssignmentResponse(user.Id, roles.ToList()));
    }

    private async Task<OperationResult<RoleAssignmentResponse>> ModifyUserRolesAsync(RoleAssignmentRequest request, bool addRoles, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return OperationResult<RoleAssignmentResponse>.Invalid("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return OperationResult<RoleAssignmentResponse>.Invalid("User id is required.");
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return OperationResult<RoleAssignmentResponse>.NotFound("User not found.");
        }

        var distinctRoles = NormalizeRoles(request.Roles);
        if (distinctRoles.Count == 0)
        {
            return OperationResult<RoleAssignmentResponse>.Invalid("At least one role must be specified.");
        }

        foreach (var roleName in distinctRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return OperationResult<RoleAssignmentResponse>.Invalid($"Role '{roleName}' does not exist.");
            }
        }

        IdentityResult operationResult;
        if (addRoles)
        {
            operationResult = await _userManager.AddToRolesAsync(user, distinctRoles);
        }
        else
        {
            operationResult = await _userManager.RemoveFromRolesAsync(user, distinctRoles);
        }

        if (!operationResult.Succeeded)
        {
            return OperationResult<RoleAssignmentResponse>.Invalid(operationResult.Errors.Select(e => e.Description).ToArray());
        }

        var updatedRoles = await _userManager.GetRolesAsync(user);
        return OperationResult<RoleAssignmentResponse>.Success(new RoleAssignmentResponse(user.Id, updatedRoles.ToList()));
    }

    private async Task<Dictionary<string, int>> GetRoleUserCountsAsync(IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (roleNames.Count == 0)
        {
            return result;
        }

        foreach (var roleName in roleNames)
        {
            var count = await GetUsersInRoleCountAsync(roleName, cancellationToken);
            result[roleName] = count;
        }

        return result;
    }

    private async Task<int> GetUsersInRoleCountAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        return users.Count;
    }

    private static IReadOnlyCollection<string> NormalizeRoles(IEnumerable<string> roles)
    {
        return roles?
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? Array.Empty<string>();
    }

    private static RoleDto ToDto(AppRole role, int userCount)
    {
        return new RoleDto(role.Id, role.Name ?? string.Empty, role.NormalizedName ?? string.Empty, userCount);
    }
}
