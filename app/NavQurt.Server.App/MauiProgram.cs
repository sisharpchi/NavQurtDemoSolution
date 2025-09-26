using Microsoft.Extensions.Logging;
using NavQurt.Server.App.Services;
using NavQurt.Server.App.ViewModels;
using NavQurt.Server.App.Views;

namespace NavQurt.Server.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

            // Options
            builder.Services.AddSingleton<ApiOptions>();

            // HttpClient (DEV handler - self-signed cert ok)
            builder.Services.AddSingleton<DevHttpHandler>();
            builder.Services.AddSingleton(sp =>
            {
                var handler = sp.GetRequiredService<DevHttpHandler>();
                return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            });

            // APIs
            builder.Services.AddSingleton<AuthApi>();

            // VMs & Pages
            builder.Services.AddTransient<SignInViewModel>();
            builder.Services.AddTransient<SignUpViewModel>();
            builder.Services.AddTransient<MeViewModel>();
            builder.Services.AddTransient<SignInPage>();
            builder.Services.AddTransient<SignUpPage>();
            builder.Services.AddTransient<MePage>();

            return builder.Build();
        }
    }
}
