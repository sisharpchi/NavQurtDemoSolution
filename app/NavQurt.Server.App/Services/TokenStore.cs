namespace NavQurt.Server.App.Services
{
    public static class TokenStore
    {
        const string KeyAccess = "auth.access";
        const string KeyRefresh = "auth.refresh";
        const string KeyExpiry = "auth.expiry";

        public static async Task SaveAsync(AuthTokens t)
        {
            await SecureStorage.SetAsync(KeyAccess, t.AccessToken);
            await SecureStorage.SetAsync(KeyRefresh, t.RefreshToken);
            await SecureStorage.SetAsync(KeyExpiry, t.ExpiresAt.ToUnixTimeSeconds().ToString());
        }

        public static async Task<AuthTokens?> LoadAsync()
        {
            var at = await SecureStorage.GetAsync(KeyAccess);
            var rt = await SecureStorage.GetAsync(KeyRefresh);
            var ex = await SecureStorage.GetAsync(KeyExpiry);
            if (string.IsNullOrEmpty(at) || string.IsNullOrEmpty(rt) || string.IsNullOrEmpty(ex)) return null;
            return new AuthTokens
            {
                AccessToken = at,
                RefreshToken = rt,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(ex))
            };
        }

        public static void Clear()
        {
            SecureStorage.Remove(KeyAccess);
            SecureStorage.Remove(KeyRefresh);
            SecureStorage.Remove(KeyExpiry);
        }
    }
}
