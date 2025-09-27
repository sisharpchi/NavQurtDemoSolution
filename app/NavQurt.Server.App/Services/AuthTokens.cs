namespace NavQurt.Server.App.Services
{
    public class AuthTokens
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; } // access token muddati (taxminiy)
    }
}
