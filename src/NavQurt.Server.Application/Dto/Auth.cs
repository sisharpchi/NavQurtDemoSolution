namespace NavQurt.Server.Application.Dto;

public record SignUpRequest(
    string UserName,
    string Password,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone);

public record SignUpResponse(
    string UserId,
    string UserName,
    string? Email,
    string? Phone,
    string? FullName,
    IReadOnlyList<string> Roles);

public record LoginRequest(string UserNameOrEmail, string Password, bool RememberMe);

public record LoginResponse(
    string UserId,
    string UserName,
    string? Email,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    bool IsActive,
    IReadOnlyList<string> Roles,
    bool RequiresTwoFactor);

public record ForgotPasswordRequest(string Email);

public record PasswordResetTokenResponse(string UserId, string Token);

public record ResetPasswordRequest(string UserId, string Token, string NewPassword);

public record ChangePasswordRequest(string UserId, string CurrentPassword, string NewPassword);

public record GenerateEmailVerificationTokenRequest(string UserId);

public record EmailVerificationTokenResponse(string UserId, string Token);

public record EmailVerificationRequest(string UserId, string Token);

public record GeneratePhoneNumberTokenRequest(string UserId, string? PhoneNumber);

public record PhoneVerificationTokenResponse(string UserId, string PhoneNumber, string Token);

public record VerifyPhoneNumberRequest(string UserId, string PhoneNumber, string Token);
