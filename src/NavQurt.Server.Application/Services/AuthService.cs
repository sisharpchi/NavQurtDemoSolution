using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Core.Entities;

namespace NavQurt.Server.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<AppRole> roleManager,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<OperationResult<SignUpResponse>> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return OperationResult<SignUpResponse>.Invalid("Username is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return OperationResult<SignUpResponse>.Invalid("Password is required.");
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.Phone,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return OperationResult<SignUpResponse>.Invalid(createResult.Errors.Select(e => e.Description).ToArray());
        }

        if (await _roleManager.RoleExistsAsync("Customer"))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "Customer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign default role to user {UserId}: {Errors}", user.Id, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = new SignUpResponse(user.Id, user.UserName!, user.Email, user.PhoneNumber, user.FullName, roles);

        return OperationResult<SignUpResponse>.Success(response);
    }

    public async Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserNameOrEmail))
        {
            return OperationResult<LoginResponse>.Invalid("Username or email is required.");
        }

        var user = await _userManager.FindByNameAsync(request.UserNameOrEmail);
        user ??= await _userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user is null)
        {
            return OperationResult<LoginResponse>.NotFound("User not found.");
        }

        if (!user.IsActive)
        {
            return OperationResult<LoginResponse>.Forbidden("User account is inactive.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return OperationResult<LoginResponse>.Forbidden("User account is locked out.");
        }

        if (signInResult.IsNotAllowed)
        {
            return OperationResult<LoginResponse>.Forbidden("User is not allowed to sign in.");
        }

        if (!signInResult.Succeeded)
        {
            return OperationResult<LoginResponse>.Invalid("Invalid username or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = new LoginResponse(
            user.Id,
            user.UserName!,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.IsActive,
            roles.ToList(),
            signInResult.RequiresTwoFactor);

        return OperationResult<LoginResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<bool>.NotFound("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return OperationResult<bool>.Invalid(result.Errors.Select(e => e.Description).ToArray());
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<PasswordResetTokenResponse>> GeneratePasswordResetTokenAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return OperationResult<PasswordResetTokenResponse>.Invalid("Email is required.");
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return OperationResult<PasswordResetTokenResponse>.NotFound("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var response = new PasswordResetTokenResponse(user.Id, token);

        return OperationResult<PasswordResetTokenResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<bool>.NotFound("User not found.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return OperationResult<bool>.Invalid(result.Errors.Select(e => e.Description).ToArray());
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<EmailVerificationTokenResponse>> GenerateEmailVerificationTokenAsync(GenerateEmailVerificationTokenRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<EmailVerificationTokenResponse>.NotFound("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return OperationResult<EmailVerificationTokenResponse>.Conflict("Email is already confirmed.");
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return OperationResult<EmailVerificationTokenResponse>.Invalid("User email is not set.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var response = new EmailVerificationTokenResponse(user.Id, token);

        return OperationResult<EmailVerificationTokenResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> ConfirmEmailAsync(EmailVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<bool>.NotFound("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return OperationResult<bool>.Success(true);
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return OperationResult<bool>.Invalid(result.Errors.Select(e => e.Description).ToArray());
        }

        return OperationResult<bool>.Success(true);
    }

    public async Task<OperationResult<PhoneVerificationTokenResponse>> GeneratePhoneNumberTokenAsync(GeneratePhoneNumberTokenRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<PhoneVerificationTokenResponse>.NotFound("User not found.");
        }

        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? user.PhoneNumber : request.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return OperationResult<PhoneVerificationTokenResponse>.Invalid("Phone number is required.");
        }

        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
        var response = new PhoneVerificationTokenResponse(user.Id, phoneNumber, token);

        return OperationResult<PhoneVerificationTokenResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> VerifyPhoneNumberAsync(VerifyPhoneNumberRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user is null)
        {
            return OperationResult<bool>.NotFound("User not found.");
        }

        var result = await _userManager.ChangePhoneNumberAsync(user, request.PhoneNumber, request.Token);
        if (!result.Succeeded)
        {
            return OperationResult<bool>.Invalid(result.Errors.Select(e => e.Description).ToArray());
        }

        return OperationResult<bool>.Success(true);
    }
}
