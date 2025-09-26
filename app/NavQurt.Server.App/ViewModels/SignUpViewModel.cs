using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavQurt.Server.App.Services;
using System.Text.Json;

namespace NavQurt.Server.App.ViewModels
{
    public partial class SignUpViewModel : ObservableObject
    {
        private readonly AuthApi _api;
        public SignUpViewModel(AuthApi api) => _api = api;

        [ObservableProperty] string userName = "";
        [ObservableProperty] string password = "";
        [ObservableProperty] string? firstName;
        [ObservableProperty] string? lastName;
        [ObservableProperty] string? email;
        [ObservableProperty] string? phone;
        [ObservableProperty] bool isBusy;
        [ObservableProperty] string? message;
        [ObservableProperty] string? error;

        [RelayCommand]
        public async Task SignUp()
        {
            try
            {
                IsBusy = true; Error = null; Message = null;
                var ok = await _api.SignUpAsync(userName, password, firstName, lastName,  email, phone, CancellationToken.None);
                if (!ok) { Error = "Sign-up failed"; return; }
                Message = "Account created! Now sign in.";
            }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }
    }
}
