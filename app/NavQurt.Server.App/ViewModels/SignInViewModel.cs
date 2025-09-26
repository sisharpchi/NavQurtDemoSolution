using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavQurt.Server.App.Services;
using NavQurt.Server.App.Views;
using System.Text.Json;

namespace NavQurt.Server.App.ViewModels
{
    public partial class SignInViewModel : ObservableObject
    {
        private readonly AuthApi _api;

        [ObservableProperty] string userName = "";
        [ObservableProperty] string password = "";
        [ObservableProperty] bool isBusy;
        [ObservableProperty] string? error;

        public SignInViewModel(AuthApi api) => _api = api;

        [RelayCommand]
        public async Task SignIn()
        {
            try
            {
                IsBusy = true; Error = null;
                var tokens = await _api.SignInAsync(userName, password, CancellationToken.None);
                if (tokens == null) { Error = "Login failed"; return; }
                await TokenStore.SaveAsync(tokens);
                await Shell.Current.GoToAsync("//me");
            }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task GoSignUp() => await Shell.Current.GoToAsync($"{nameof(SignUpPage)}");
    }
}
