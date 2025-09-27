using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace NavQurt.Server.App.Services;

public class GeneralApi
{
    private readonly HttpClient _http;
    private readonly ApiOptions _options;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public GeneralApi(HttpClient http, ApiOptions options)
    {
        _http = http;
        _options = options;
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(_options.BaseUrl);
        }
    }

    public async Task<SignUpResponse> SignUpAsync(string userName, string password, string? firstName, string? lastName,
        string? email, string? phone, CancellationToken cancellationToken)
    {
        var payload = new SignUpRequest(userName, password, firstName, lastName, email, phone);
        return await PostAsync<SignUpRequest, SignUpResponse>("api/v1/auth/sign-up", payload, cancellationToken, false);
    }

    public async Task<LoginResponse> LoginAsync(string userNameOrEmail, string password, bool rememberMe,
        CancellationToken cancellationToken)
    {
        var payload = new LoginRequest(userNameOrEmail, password, rememberMe);
        var response = await PostAsync<LoginRequest, LoginResponse>("api/v1/auth/login", payload, cancellationToken, false);

        var tokens = new AuthTokens
        {
            UserId = response.UserId,
            UserName = response.UserName,
            AccessToken = response.Tokens.AccessToken,
            AccessTokenExpiresAt = response.Tokens.AccessTokenExpiresAt,
            RefreshToken = response.Tokens.RefreshToken,
            RefreshTokenExpiresAt = response.Tokens.RefreshTokenExpiresAt
        };

        await TokenStore.SaveAsync(tokens);
        return response;
    }

    public async Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var payload = new RefreshTokenRequest(refreshToken);
        var tokenPair = await PostAsync<RefreshTokenRequest, TokenPair>("api/v1/auth/refresh", payload, cancellationToken, false);

        var tokens = new AuthTokens
        {
            AccessToken = tokenPair.AccessToken,
            AccessTokenExpiresAt = tokenPair.AccessTokenExpiresAt,
            RefreshToken = tokenPair.RefreshToken,
            RefreshTokenExpiresAt = tokenPair.RefreshTokenExpiresAt
        };

        var existing = await TokenStore.LoadAsync();
        if (existing is not null)
        {
            tokens.UserId = existing.UserId;
            tokens.UserName = existing.UserName;
        }

        await TokenStore.SaveAsync(tokens);
        return tokens;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        var tokens = await TokenStore.LoadAsync() ?? throw new InvalidOperationException("Not signed in.");
        var payload = new LogoutRequest(tokens.RefreshToken);
        await PostAsync<LogoutRequest, bool>("api/v1/auth/logout", payload, cancellationToken, authorize: true);
        TokenStore.Clear();
    }

    public Task<PasswordResetTokenResponse> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
    {
        var payload = new ForgotPasswordRequest(email);
        return PostAsync<ForgotPasswordRequest, PasswordResetTokenResponse>("api/v1/auth/forgot-password", payload,
            cancellationToken, false);
    }

    public Task<bool> ResetPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken)
    {
        var payload = new ResetPasswordRequest(userId, token, newPassword);
        return PostAsync<ResetPasswordRequest, bool>("api/v1/auth/reset-password", payload, cancellationToken, false);
    }

    public Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword,
        CancellationToken cancellationToken)
    {
        var payload = new ChangePasswordRequest(userId, currentPassword, newPassword);
        return PostAsync<ChangePasswordRequest, bool>("api/v1/auth/change-password", payload, cancellationToken, true);
    }

    public Task<EmailVerificationTokenResponse> GenerateEmailTokenAsync(string userId, CancellationToken cancellationToken)
    {
        var payload = new GenerateEmailVerificationTokenRequest(userId);
        return PostAsync<GenerateEmailVerificationTokenRequest, EmailVerificationTokenResponse>("api/v1/auth/email/token",
            payload, cancellationToken, true);
    }

    public Task<bool> VerifyEmailAsync(string userId, string token, CancellationToken cancellationToken)
    {
        var payload = new EmailVerificationRequest(userId, token);
        return PostAsync<EmailVerificationRequest, bool>("api/v1/auth/email/verify", payload, cancellationToken, false);
    }

    public Task<PhoneVerificationTokenResponse> GeneratePhoneTokenAsync(string userId, string? phoneNumber,
        CancellationToken cancellationToken)
    {
        var payload = new GeneratePhoneNumberTokenRequest(userId, phoneNumber);
        return PostAsync<GeneratePhoneNumberTokenRequest, PhoneVerificationTokenResponse>("api/v1/auth/phone/token",
            payload, cancellationToken, true);
    }

    public Task<bool> VerifyPhoneAsync(string userId, string phoneNumber, string token, CancellationToken cancellationToken)
    {
        var payload = new VerifyPhoneNumberRequest(userId, phoneNumber, token);
        return PostAsync<VerifyPhoneNumberRequest, bool>("api/v1/auth/phone/verify", payload, cancellationToken, false);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken)
        => await GetAsync<List<UserDto>>("api/v1/users", cancellationToken);

    public Task<UserDto> GetUserAsync(string userId, CancellationToken cancellationToken)
        => GetAsync<UserDto>($"api/v1/users/{userId}", cancellationToken);

    public Task<UserDto> UpdateUserProfileAsync(string userId, string? userName, string? firstName, string? lastName,
        string? email, string? phone, bool? isActive, CancellationToken cancellationToken)
    {
        var payload = new UpdateUserProfileRequest(userId, userName, firstName, lastName, email, phone, isActive);
        return PutAsync<UpdateUserProfileRequest, UserDto>($"api/v1/users/{userId}", payload, cancellationToken);
    }

    public Task<RoleAssignmentResponse> UpdateUserRolesAsync(string userId, IList<string> roles,
        CancellationToken cancellationToken)
    {
        var payload = new UpdateUserRolesRequest(userId, roles);
        return PutAsync<UpdateUserRolesRequest, RoleAssignmentResponse>($"api/v1/users/{userId}/roles", payload, cancellationToken);
    }

    public Task<UserDto> SetUserStatusAsync(string userId, bool isActive, CancellationToken cancellationToken)
    {
        var payload = new UpdateUserStatusRequest(userId, isActive);
        return PutAsync<UpdateUserStatusRequest, UserDto>($"api/v1/users/{userId}/status", payload, cancellationToken);
    }

    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/users/{userId}");
        await SendAuthorizedAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
        => await GetAsync<List<RoleDto>>("api/v1/roles", cancellationToken);

    public Task<RoleDto> GetRoleAsync(string roleId, CancellationToken cancellationToken)
        => GetAsync<RoleDto>($"api/v1/roles/{roleId}", cancellationToken);

    public Task<RoleDto> CreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        var payload = new CreateRoleRequest(name);
        return PostAsync<CreateRoleRequest, RoleDto>("api/v1/roles", payload, cancellationToken, true);
    }

    public Task<RoleDto> UpdateRoleAsync(string roleId, string name, CancellationToken cancellationToken)
    {
        var payload = new UpdateRoleRequest(name);
        return PutAsync<UpdateRoleRequest, RoleDto>($"api/v1/roles/{roleId}", payload, cancellationToken);
    }

    public async Task DeleteRoleAsync(string roleId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v1/roles/{roleId}");
        await SendAuthorizedAsync(request, cancellationToken);
    }

    public Task<RoleAssignmentResponse> AssignRolesAsync(string userId, IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var payload = new RoleAssignmentRequest(userId, roles);
        return PostAsync<RoleAssignmentRequest, RoleAssignmentResponse>("api/v1/roles/assign", payload, cancellationToken, true);
    }

    public Task<RoleAssignmentResponse> RemoveRolesAsync(string userId, IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var payload = new RoleAssignmentRequest(userId, roles);
        return PostAsync<RoleAssignmentRequest, RoleAssignmentResponse>("api/v1/roles/remove", payload, cancellationToken, true);
    }

    public Task<RoleAssignmentResponse> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
        => GetAsync<RoleAssignmentResponse>($"api/v1/roles/users/{userId}", cancellationToken);

    public Task<AuthTokens?> GetSavedTokensAsync() => TokenStore.LoadAsync();

    private async Task<TResponse> GetAsync<TResponse>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        await EnsureAuthorizationAsync(request, cancellationToken);
        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken))!;
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest payload,
        CancellationToken cancellationToken, bool authorize)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };

        if (authorize)
        {
            await EnsureAuthorizationAsync(request, cancellationToken);
        }

        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return default!;
        }

        return (await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken))!;
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };

        await EnsureAuthorizationAsync(request, cancellationToken);
        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return (await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken))!;
    }

    private async Task EnsureAuthorizationAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokens = await TokenStore.LoadAsync();
        if (tokens is null)
        {
            throw new InvalidOperationException("You must sign in first.");
        }

        if (tokens.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1))
        {
            if (tokens.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
            {
                TokenStore.Clear();
                throw new InvalidOperationException("Session expired. Please sign in again.");
            }

            var refreshed = await RefreshAsync(tokens.RefreshToken, cancellationToken);
            tokens = refreshed;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
    }

    private async Task SendAuthorizedAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await EnsureAuthorizationAsync(request, cancellationToken);
        var response = await _http.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await ExtractErrorAsync(response, cancellationToken);
        throw new InvalidOperationException(error);
    }

    private async Task<string> ExtractErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                var problem = JsonSerializer.Deserialize<ErrorEnvelope>(body, _jsonOptions);
                if (problem?.Errors?.Count > 0)
                {
                    return string.Join("\n", problem.Errors);
                }
            }
            catch
            {
                // ignored - fallback below
            }
        }

        return $"Request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}).";
    }

    private sealed record ErrorEnvelope(IReadOnlyList<string>? Errors);
}

public record SignUpRequest(string UserName, string Password, string? FirstName, string? LastName, string? Email, string? Phone);

public record SignUpResponse(string UserId, string UserName, string? Email, string? Phone, string? FullName,
    IList<string> Roles);

public record LoginRequest(string UserNameOrEmail, string Password, bool RememberMe);

public record TokenPair(string AccessToken, DateTimeOffset AccessTokenExpiresAt, string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public record LoginResponse(string UserId, string UserName, string? Email, string? PhoneNumber, bool EmailConfirmed,
    bool PhoneNumberConfirmed, bool IsActive, IReadOnlyList<string> Roles, bool RequiresTwoFactor, TokenPair Tokens);

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

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record UserDto(string Id, string UserName, string? FirstName, string? LastName, string? Email, bool EmailConfirmed,
    string? PhoneNumber, bool PhoneNumberConfirmed, bool IsActive, string? FullName, DateTime CreatedAt, IList<string> Roles);

public record UpdateUserProfileRequest(string? UserId, string? UserName, string? FirstName, string? LastName, string? Email,
    string? PhoneNumber, bool? IsActive);

public record UpdateUserRolesRequest(string? UserId, IList<string> Roles);

public record UpdateUserStatusRequest(string? UserId, bool IsActive);

public record RoleDto(string Id, string Name, string NormalizedName, int UserCount);

public record CreateRoleRequest(string Name);

public record UpdateRoleRequest(string Name);

public record RoleAssignmentRequest(string UserId, IReadOnlyCollection<string> Roles);

public record RoleAssignmentResponse(string UserId, IReadOnlyList<string> Roles);
