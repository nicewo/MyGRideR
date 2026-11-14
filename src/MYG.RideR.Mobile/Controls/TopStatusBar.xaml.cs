using MYG.RideR.Mobile.Views;

namespace MYG.RideR.Mobile.Controls;

public partial class TopStatusBar : ContentView
{
    public TopStatusBar()
    {
        InitializeComponent();
        try
        {
            var level = Battery.Default.ChargeLevel;
            if (level >= 0)
            {
                BatteryLabel.Text = $"%{(int)Math.Round(level * 100)}";
            }
        }
        catch (FeatureNotSupportedException)
        {
        }
    }

    private async void OnMenuClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(StatisticsPage));
}
