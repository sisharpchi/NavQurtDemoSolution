using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NavQurt.Server.App.Services;
using NavQurt.Server.App.Views;

namespace NavQurt.Server.App.ViewModels;

public partial class SignInViewModel : ObservableObject
{
    private readonly GeneralApi _api;

    [ObservableProperty]
    private string userNameOrEmail = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe = true;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string? errorMessage;

    public SignInViewModel(GeneralApi api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = null;

            var response = await _api.LoginAsync(UserNameOrEmail.Trim(), Password, RememberMe, CancellationToken.None);
            StatusMessage = $"Welcome {response.UserName}!";
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoSignUpAsync()
        => Shell.Current.GoToAsync("//signup");

    [RelayCommand]
    private async Task ClearAsync()
    {
        UserNameOrEmail = string.Empty;
        Password = string.Empty;
        RememberMe = true;
        StatusMessage = null;
        ErrorMessage = null;
        await Task.CompletedTask;
    }
}
