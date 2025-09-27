namespace NavQurt.Server.App.Services;

public class AuthTokens
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
        = DateTimeOffset.UtcNow;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
        = DateTimeOffset.UtcNow;
}
