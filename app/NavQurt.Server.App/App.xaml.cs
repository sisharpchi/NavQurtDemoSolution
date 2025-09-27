using Microsoft.Maui.Controls;
using NavQurt.Server.App.Services;

namespace NavQurt.Server.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();

        Dispatcher.Dispatch(async () =>
        {
            var tokens = await TokenStore.LoadAsync();
            var route = tokens is null ? "//signin" : "//dashboard";
            await Shell.Current.GoToAsync(route);
        });
    }
}
