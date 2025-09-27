using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NavQurt.Server.App.Services
{
    public class AuthApi
    {
        private readonly HttpClient _http;
        private readonly ApiOptions _opt;

        public AuthApi(HttpClient http, ApiOptions opt)
        {
            _http = http;
            _opt = opt;
            _http.BaseAddress = new Uri(_opt.BaseUrl);
        }

        record SignUpRequest(string UserName, string Password, string? FirstName, string? LastName, string? Email, string? Phone);
        public record TokenResponse(string access_token, string token_type, int expires_in, string refresh_token);

        // --- SignUp ---
        public async Task<bool> SignUpAsync(string username, string password, string? firstName, string? lastName, string? email, string? phone, CancellationToken ct)
        {
            var dto = new SignUpRequest(username, password, firstName, lastName, email, phone);
            var json = JsonSerializer.Serialize(dto);
            var resp = await _http.PostAsync("api/v1/auth/sign-up",
                new StringContent(json, Encoding.UTF8, "application/json"), ct);
            return resp.IsSuccessStatusCode;
        }

        // --- SignIn (password grant) ---
        public async Task<AuthTokens?> SignInAsync(string username, string password, CancellationToken ct)
        {
            var kv = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = username,
                ["password"] = password,
                ["scope"] = "offline_access api"
            };
            var resp = await _http.PostAsync("connect/token", new FormUrlEncodedContent(kv), ct);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            var token = JsonSerializer.Deserialize<TokenResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return new AuthTokens
            {
                AccessToken = token.access_token,
                RefreshToken = token.refresh_token,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.expires_in - 30)
            };
        }

        // --- Refresh ---
        public async Task<AuthTokens?> RefreshAsync(string refreshToken, CancellationToken ct)
        {
            var kv = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            };
            var resp = await _http.PostAsync("connect/token", new FormUrlEncodedContent(kv), ct);
            if (!resp.IsSuccessStatusCode) return null;

            var body = await resp.Content.ReadAsStringAsync(ct);
            var token = JsonSerializer.Deserialize<TokenResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            return new AuthTokens
            {
                AccessToken = token.access_token,
                RefreshToken = token.refresh_token,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.expires_in - 30)
            };
        }

        // --- Me ---
        public async Task<string?> MeRawAsync(string accessToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "api/v1/me");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var resp = await _http.SendAsync(req, ct);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
        }
    }
}
