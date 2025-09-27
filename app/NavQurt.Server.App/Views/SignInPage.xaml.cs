using Microsoft.Maui.Controls;
using NavQurt.Server.App.ViewModels;

namespace NavQurt.Server.App.Views;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}