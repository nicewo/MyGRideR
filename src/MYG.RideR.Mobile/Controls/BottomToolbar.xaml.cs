using MYG.RideR.Mobile.Views;

namespace MYG.RideR.Mobile.Controls;

public partial class BottomToolbar : ContentView
{
    public BottomToolbar()
    {
        InitializeComponent();
    }

    private async void DashboardTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//dashboard");
    }

    private async void TripsTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TripsPage));
    }

    private async void StatisticsTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StatisticsPage));
    }

    private async void SettingsTapped(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}
