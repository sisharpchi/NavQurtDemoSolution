using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NavQurt.Server.App.Services;

namespace NavQurt.Server.App.ViewModels;

public partial class SignUpViewModel : ObservableObject
{
    private readonly GeneralApi _api;

    [ObservableProperty]
    private string userName = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string? firstName;

    [ObservableProperty]
    private string? lastName;

    [ObservableProperty]
    private string? email;

    [ObservableProperty]
    private string? phone;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? infoMessage;

    [ObservableProperty]
    private string? errorMessage;

    public SignUpViewModel(GeneralApi api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task SignUpAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            InfoMessage = null;

            var response = await _api.SignUpAsync(UserName.Trim(), Password, FirstName, LastName, Email, Phone,
                CancellationToken.None);

            InfoMessage = $"Account created for {response.UserName}. Roles: {string.Join(", ", response.Roles)}";
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
    private Task GoSignInAsync()
        => Shell.Current.GoToAsync("//signin");

    [RelayCommand]
    private async Task ClearAsync()
    {
        UserName = string.Empty;
        Password = string.Empty;
        FirstName = null;
        LastName = null;
        Email = null;
        Phone = null;
        InfoMessage = null;
        ErrorMessage = null;
        await Task.CompletedTask;
    }
}
