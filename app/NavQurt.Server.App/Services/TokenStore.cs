namespace NavQurt.Server.App.Services;

public static class TokenStore
{
    private const string KeyUserId = "auth.user";
    private const string KeyUserName = "auth.userName";
    private const string KeyAccess = "auth.access";
    private const string KeyRefresh = "auth.refresh";
    private const string KeyAccessExpiry = "auth.access.expiry";
    private const string KeyRefreshExpiry = "auth.refresh.expiry";

    public static async Task SaveAsync(AuthTokens tokens)
    {
        await SecureStorage.SetAsync(KeyUserId, tokens.UserId);
        await SecureStorage.SetAsync(KeyUserName, tokens.UserName);
        await SecureStorage.SetAsync(KeyAccess, tokens.AccessToken);
        await SecureStorage.SetAsync(KeyRefresh, tokens.RefreshToken);
        await SecureStorage.SetAsync(KeyAccessExpiry, tokens.AccessTokenExpiresAt.ToUnixTimeSeconds().ToString());
        await SecureStorage.SetAsync(KeyRefreshExpiry, tokens.RefreshTokenExpiresAt.ToUnixTimeSeconds().ToString());
    }

    public static async Task<AuthTokens?> LoadAsync()
    {
        var userId = await SecureStorage.GetAsync(KeyUserId);
        var userName = await SecureStorage.GetAsync(KeyUserName);
        var access = await SecureStorage.GetAsync(KeyAccess);
        var refresh = await SecureStorage.GetAsync(KeyRefresh);
        var accessExpiry = await SecureStorage.GetAsync(KeyAccessExpiry);
        var refreshExpiry = await SecureStorage.GetAsync(KeyRefreshExpiry);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(access) ||
            string.IsNullOrWhiteSpace(refresh) || string.IsNullOrWhiteSpace(accessExpiry) ||
            string.IsNullOrWhiteSpace(refreshExpiry))
        {
            return null;
        }

        if (!long.TryParse(accessExpiry, out var accessUnix) || !long.TryParse(refreshExpiry, out var refreshUnix))
        {
            return null;
        }

        return new AuthTokens
        {
            UserId = userId,
            UserName = userName ?? string.Empty,
            AccessToken = access,
            AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessUnix),
            RefreshToken = refresh,
            RefreshTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(refreshUnix)
        };
    }

    public static void Clear()
    {
        SecureStorage.Remove(KeyUserId);
        SecureStorage.Remove(KeyUserName);
        SecureStorage.Remove(KeyAccess);
        SecureStorage.Remove(KeyRefresh);
        SecureStorage.Remove(KeyAccessExpiry);
        SecureStorage.Remove(KeyRefreshExpiry);
    }
}
