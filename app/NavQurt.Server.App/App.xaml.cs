using NavQurt.Server.App.Views;

namespace NavQurt.Server.App
{
    public partial class App : Application
    {
        public App(SignInPage page)
        {
            InitializeComponent();
            MainPage = new AppShell();
            // default nav
            Shell.Current.GoToAsync("//signin");
        }
    }
}
