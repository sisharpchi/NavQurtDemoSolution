using NavQurt.Server.App.ViewModels;

namespace NavQurt.Server.App.Views;

public partial class SignUpPage : ContentPage
{
    public SignUpPage(SignUpViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}