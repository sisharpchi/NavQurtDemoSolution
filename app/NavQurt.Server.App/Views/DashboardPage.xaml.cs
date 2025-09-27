//using CommunityToolkit.Mvvm.Input;
//using Microsoft.Maui.Controls;
//using NavQurt.Server.App.ViewModels;

//namespace NavQurt.Server.App.Views;

//public partial class DashboardPage : ContentPage
//{
//    public DashboardPage(DashboardViewModel viewModel)
//    {
//        InitializeComponent();
//        BindingContext = viewModel;
//    }

//    protected override async void OnAppearing()
//    {
//        base.OnAppearing();

//        if (BindingContext is DashboardViewModel vm && vm.RefreshSessionCommand is IAsyncRelayCommand command)
//        {
//            await command.ExecuteAsync(null);
//        }
//    }
//}
