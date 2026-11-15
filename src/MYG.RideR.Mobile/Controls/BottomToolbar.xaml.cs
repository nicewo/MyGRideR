using MYG.RideR.Mobile.Views;

namespace MYG.RideR.Mobile.Controls;

public partial class BottomToolbar : ContentView
{
    private bool _voiceAlerts = true;

    public BottomToolbar()
    {
        InitializeComponent();
    }

    private async void RouteClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(TripsPage));

    private async void SettingsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(SettingsPage));

    private async void PhotoClicked(object? sender, EventArgs e) =>
        await Shell.Current.CurrentPage.DisplayAlertAsync("Fotoğraf Ekle", "Bu özellik yakında eklenecek.", "Tamam");

    private async void AlertClicked(object? sender, EventArgs e)
    {
        _voiceAlerts = !_voiceAlerts;
        await Shell.Current.CurrentPage.DisplayAlertAsync("Sesli Uyarı", _voiceAlerts ? "Sesli uyarılar açık." : "Sesli uyarılar kapalı.", "Tamam");
    }
}
