using NavQurt.Server.Application.Dto;

namespace NavQurt.Server.Application.Interfaces;

public interface IAuthService
{
    Task<OperationResult<SignUpResponse>> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<PasswordResetTokenResponse>> GeneratePasswordResetTokenAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<EmailVerificationTokenResponse>> GenerateEmailVerificationTokenAsync(GenerateEmailVerificationTokenRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> ConfirmEmailAsync(EmailVerificationRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<PhoneVerificationTokenResponse>> GeneratePhoneNumberTokenAsync(GeneratePhoneNumberTokenRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<bool>> VerifyPhoneNumberAsync(VerifyPhoneNumberRequest request, CancellationToken cancellationToken = default);
}
