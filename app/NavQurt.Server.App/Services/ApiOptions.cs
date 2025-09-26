namespace NavQurt.Server.App.Services
{
    public class ApiOptions
    {
        // ANDROID emulator uchun 10.0.2.2 ni ishlating.
        #if ANDROID
            public string BaseUrl { get; set; } = "https://10.0.2.2:7145/";
        #else
            public string BaseUrl { get; set; } = "https://localhost:7145/";
        #endif
    }
}
