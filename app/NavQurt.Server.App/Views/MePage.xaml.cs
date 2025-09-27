using NavQurt.Server.App.ViewModels;

namespace NavQurt.Server.App.Views;

public partial class MePage : ContentPage
{
    public MePage(MeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnAppearing(object sender, EventArgs e)
    {
        if (BindingContext is MeViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}