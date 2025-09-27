using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using System.Security.Claims;

namespace NavQurt.Server.Web.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.SignUpAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.LoginAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.GeneratePasswordResetTokenAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (!IsSameUserOrAdmin(request.UserId))
        {
            return Forbid();
        }

        var result = await _authService.ChangePasswordAsync(request, cancellationToken);
        return MapResult(result, _ => NoContent());
    }

    [HttpPost("email/token")]
    [Authorize]
    public async Task<IActionResult> GenerateEmailToken([FromBody] GenerateEmailVerificationTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (!IsSameUserOrAdmin(request.UserId))
        {
            return Forbid();
        }

        var result = await _authService.GenerateEmailVerificationTokenAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("email/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.ConfirmEmailAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("phone/token")]
    [Authorize]
    public async Task<IActionResult> GeneratePhoneToken([FromBody] GeneratePhoneNumberTokenRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        if (!IsSameUserOrAdmin(request.UserId))
        {
            return Forbid();
        }

        var result = await _authService.GeneratePhoneNumberTokenAsync(request, cancellationToken);
        return MapResult(result);
    }

    [HttpPost("phone/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneNumberRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request body is required." } });
        }

        var result = await _authService.VerifyPhoneNumberAsync(request, cancellationToken);
        return MapResult(result);
    }

    private IActionResult MapResult<T>(OperationResult<T> result, Func<T?, IActionResult>? successFactory = null)
    {
        if (result.Succeeded)
        {
            return successFactory?.Invoke(result.Data) ?? Ok(result.Data);
        }

        return result.Status switch
        {
            OperationStatus.NotFound => NotFound(new { errors = result.Errors }),
            OperationStatus.Conflict => Conflict(new { errors = result.Errors }),
            OperationStatus.Forbidden => Forbid(),
            OperationStatus.Error => StatusCode(StatusCodes.Status500InternalServerError, new { errors = result.Errors }),
            _ => BadRequest(new { errors = result.Errors })
        };
    }

    private bool IsSameUserOrAdmin(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            return true;
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.Equals(currentUserId, userId, StringComparison.Ordinal);
    }
}
