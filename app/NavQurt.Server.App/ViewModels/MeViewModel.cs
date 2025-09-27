using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NavQurt.Server.App.Services;
using System.Text.Json;

namespace NavQurt.Server.App.ViewModels
{
    public partial class MeViewModel : ObservableObject
    {
        private readonly AuthApi _api;
        [ObservableProperty] string? meJson;
        [ObservableProperty] string? error;

        public MeViewModel(AuthApi api) => _api = api;

        [RelayCommand]
        public async Task Load()
        {
            try
            {
                Error = null;
                var t = await TokenStore.LoadAsync();
                if (t == null) { Error = "Not signed in"; return; }

                // Refresh agar muddat tugayotgan bo‘lsa:
                if (t.ExpiresAt < DateTimeOffset.UtcNow.AddSeconds(10))
                {
                    var refreshed = await _api.RefreshAsync(t.RefreshToken, CancellationToken.None);
                    if (refreshed != null) { t = refreshed; await TokenStore.SaveAsync(t); }
                }

                var json = await _api.MeRawAsync(t.AccessToken, CancellationToken.None);
                if (json == null) { Error = "Unauthorized"; return; }
                MeJson = JsonSerializer.Serialize(JsonSerializer.Deserialize<object>(json), new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex) { Error = ex.Message; }
        }

        [RelayCommand]
        public async Task Logout()
        {
            TokenStore.Clear();
            await Shell.Current.GoToAsync("//signin");
        }
    }
}
