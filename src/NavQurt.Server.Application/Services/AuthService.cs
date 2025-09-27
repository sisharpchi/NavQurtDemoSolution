using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NavQurt.Server.Application.Dto;
using NavQurt.Server.Application.Interfaces;
using NavQurt.Server.Application.Options;
using NavQurt.Server.Core.Constants;
using NavQurt.Server.Core.Entities;
using NavQurt.Server.Infrastructure.Data;

namespace NavQurt.Server.Application.Services;

public class AuthService : IAuthService
{
    private const string RefreshTokenProvider = "NavQurt.RefreshTokens";

    private static readonly JsonSerializerOptions RefreshTokenSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly MainDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        RoleManager<AppRole> roleManager,
        MainDbContext dbContext,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        if (string.IsNullOrWhiteSpace(_jwtOptions.Key))
        {
            throw new InvalidOperationException("JWT signing key must be configured.");
        }

        _timeProvider = timeProvider;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
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

        if (await _roleManager.RoleExistsAsync(RoleConstants.User))
        {
            var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.User);
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
        var response = await BuildLoginResponseAsync(user, roles, null, cancellationToken, signInResult.RequiresTwoFactor);

        return OperationResult<LoginResponse>.Success(response);
    }

    public async Task<OperationResult<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return OperationResult<LoginResponse>.Invalid("Refresh token is required.");
        }

        if (!TryParseRefreshToken(request.RefreshToken, out var tokenId, out var tokenSecret))
        {
            return OperationResult<LoginResponse>.Invalid("Refresh token is malformed.");
        }

        var refreshTokenEntity = await GetRefreshTokenEntityAsync(tokenId, cancellationToken);
        if (refreshTokenEntity is null)
        {
            return OperationResult<LoginResponse>.Invalid("Refresh token is invalid.");
        }

        var payload = DeserializePayload(refreshTokenEntity.Value);
        if (payload is null)
        {
            await RemoveRefreshTokenAsync(refreshTokenEntity, cancellationToken);
            return OperationResult<LoginResponse>.Error("Stored refresh token payload is invalid.");
        }

        var now = _timeProvider.GetUtcNow();
        if (payload.ExpiresAt <= now)
        {
            await RevokeRefreshTokenAsync(refreshTokenEntity, null, now, cancellationToken);
            return OperationResult<LoginResponse>.Invalid("Refresh token has expired.");
        }

        var providedHash = ComputeHash(tokenSecret);
        if (!TryDecodeHash(payload.TokenHash, out var storedHash) || !TryDecodeHash(providedHash, out var providedHashBytes) || !CryptographicOperations.FixedTimeEquals(storedHash, providedHashBytes))
        {
            await RevokeRefreshTokenChainAsync(refreshTokenEntity.UserId, cancellationToken);
            return OperationResult<LoginResponse>.Forbidden("Refresh token reuse detected.");
        }

        if (payload.RevokedAt.HasValue)
        {
            await RevokeRefreshTokenChainAsync(refreshTokenEntity.UserId, cancellationToken);
            return OperationResult<LoginResponse>.Forbidden("Refresh token has already been used.");
        }

        var user = await _userManager.FindByIdAsync(refreshTokenEntity.UserId);
        if (user is null || !user.IsActive)
        {
            await RevokeRefreshTokenChainAsync(refreshTokenEntity.UserId, cancellationToken);
            return OperationResult<LoginResponse>.Forbidden("User is no longer allowed to sign in.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var response = await BuildLoginResponseAsync(user, roles, refreshTokenEntity, cancellationToken);

        return OperationResult<LoginResponse>.Success(response);
    }

    public async Task<OperationResult<bool>> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return OperationResult<bool>.Invalid("Refresh token is required.");
        }

        if (!TryParseRefreshToken(request.RefreshToken, out var tokenId, out _))
        {
            return OperationResult<bool>.Invalid("Refresh token is malformed.");
        }

        var refreshTokenEntity = await GetRefreshTokenEntityAsync(tokenId, cancellationToken);
        if (refreshTokenEntity is null)
        {
            return OperationResult<bool>.Success(true);
        }

        await RevokeRefreshTokenAsync(refreshTokenEntity, null, _timeProvider.GetUtcNow(), cancellationToken);
        return OperationResult<bool>.Success(true);
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

    private async Task<LoginResponse> BuildLoginResponseAsync(AppUser user, IList<string> roles, IdentityUserToken<string>? refreshTokenToReplace, CancellationToken cancellationToken, bool requiresTwoFactor = false)
    {
        await PruneExpiredTokensAsync(user.Id, cancellationToken);

        var tokens = await IssueTokenPairAsync(user, roles, null, refreshTokenToReplace, cancellationToken);

        return new LoginResponse(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed,
            user.IsActive,
            roles.ToList(),
            requiresTwoFactor,
            tokens);
    }

    private async Task<TokenPair> IssueTokenPairAsync(AppUser user, IList<string> roles, string? ipAddress, IdentityUserToken<string>? refreshTokenToReplace, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var accessExpiresAt = now.Add(_jwtOptions.AccessTokenLifetime);
        var claims = CreateClaims(user, roles, now);

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_jwtOptions.Issuer) ? null : _jwtOptions.Issuer,
            audience: string.IsNullOrWhiteSpace(_jwtOptions.Audience) ? null : _jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = _tokenHandler.WriteToken(jwt);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, cancellationToken);

        if (refreshTokenToReplace is not null)
        {
            await RevokeRefreshTokenAsync(refreshTokenToReplace, refreshToken.TokenId, now, cancellationToken);
        }

        return new TokenPair(accessToken, accessExpiresAt, refreshToken.Token, refreshToken.ExpiresAt);
    }

    private static List<Claim> CreateClaims(AppUser user, IList<string> roles, DateTimeOffset issuedAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.PhoneNumber, user.PhoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    private async Task<RefreshTokenResult> CreateRefreshTokenAsync(string userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var tokenId = Guid.NewGuid().ToString("N");
        var secretBytes = RandomNumberGenerator.GetBytes(64);
        var tokenSecret = WebEncoders.Base64UrlEncode(secretBytes);
        var now = _timeProvider.GetUtcNow();

        var payload = new RefreshTokenPayload
        {
            TokenHash = ComputeHash(tokenSecret),
            CreatedAt = now,
            ExpiresAt = now.Add(_jwtOptions.RefreshTokenLifetime),
            IpAddress = ipAddress
        };

        var tokenEntity = new IdentityUserToken<string>
        {
            UserId = userId,
            LoginProvider = RefreshTokenProvider,
            Name = tokenId,
            Value = SerializePayload(payload)
        };

        _dbContext.Set<IdentityUserToken<string>>().Add(tokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(tokenId, $"{tokenId}.{tokenSecret}", payload.ExpiresAt);
    }

    private async Task PruneExpiredTokensAsync(string userId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var tokens = await _dbContext.Set<IdentityUserToken<string>>()
            .Where(t => t.UserId == userId && t.LoginProvider == RefreshTokenProvider)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var token in tokens)
        {
            var payload = DeserializePayload(token.Value);
            if (payload is null || payload.ExpiresAt <= now)
            {
                _dbContext.Remove(token);
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<IdentityUserToken<string>?> GetRefreshTokenEntityAsync(string tokenId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<IdentityUserToken<string>>()
            .AsTracking()
            .FirstOrDefaultAsync(t => t.LoginProvider == RefreshTokenProvider && t.Name == tokenId, cancellationToken);
    }

    private async Task RemoveRefreshTokenAsync(IdentityUserToken<string> tokenEntity, CancellationToken cancellationToken)
    {
        _dbContext.Remove(tokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeRefreshTokenAsync(IdentityUserToken<string> tokenEntity, string? replacedByTokenId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        var payload = DeserializePayload(tokenEntity.Value);
        if (payload is null)
        {
            _dbContext.Remove(tokenEntity);
        }
        else
        {
            payload.RevokedAt = revokedAt;
            payload.ReplacedByTokenId = replacedByTokenId;
            tokenEntity.Value = SerializePayload(payload);
            _dbContext.Update(tokenEntity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeRefreshTokenChainAsync(string userId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var tokens = await _dbContext.Set<IdentityUserToken<string>>()
            .Where(t => t.UserId == userId && t.LoginProvider == RefreshTokenProvider)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var token in tokens)
        {
            var payload = DeserializePayload(token.Value);
            if (payload is null)
            {
                _dbContext.Remove(token);
                changed = true;
                continue;
            }

            if (!payload.RevokedAt.HasValue)
            {
                payload.RevokedAt = now;
                token.Value = SerializePayload(payload);
                _dbContext.Update(token);
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool TryParseRefreshToken(string refreshToken, out string tokenId, out string tokenSecret)
    {
        tokenId = string.Empty;
        tokenSecret = string.Empty;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var parts = refreshToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        tokenId = parts[0];
        tokenSecret = parts[1];
        return !string.IsNullOrWhiteSpace(tokenId) && !string.IsNullOrWhiteSpace(tokenSecret);
    }

    private static RefreshTokenPayload? DeserializePayload(string payload)
    {
        return JsonSerializer.Deserialize<RefreshTokenPayload>(payload, RefreshTokenSerializerOptions);
    }

    private static string SerializePayload(RefreshTokenPayload payload)
    {
        return JsonSerializer.Serialize(payload, RefreshTokenSerializerOptions);
    }

    private static string ComputeHash(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static bool TryDecodeHash(string value, out byte[] buffer)
    {
        buffer = Array.Empty<byte>();
        try
        {
            buffer = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record RefreshTokenResult(string TokenId, string Token, DateTimeOffset ExpiresAt);

    private sealed class RefreshTokenPayload
    {
        public string TokenHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public string? ReplacedByTokenId { get; set; }

        public string? IpAddress { get; set; }
    }
}
