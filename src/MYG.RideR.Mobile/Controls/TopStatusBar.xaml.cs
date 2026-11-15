using MYG.RideR.Mobile.Views;

namespace MYG.RideR.Mobile.Controls;

public partial class TopStatusBar : ContentView
{
    public static readonly BindableProperty GpsSummaryTextProperty =
        BindableProperty.Create(nameof(GpsSummaryText), typeof(string), typeof(TopStatusBar), "GPS yok · -- · ±--");

    public static readonly BindableProperty GpsQualityColorProperty =
        BindableProperty.Create(nameof(GpsQualityColor), typeof(Color), typeof(TopStatusBar), Colors.Gray);

    public string GpsSummaryText
    {
        get => (string)GetValue(GpsSummaryTextProperty);
        set => SetValue(GpsSummaryTextProperty, value);
    }

    public Color GpsQualityColor
    {
        get => (Color)GetValue(GpsQualityColorProperty);
        set => SetValue(GpsQualityColorProperty, value);
    }

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
        catch (Exception ex) when (ex is FeatureNotSupportedException or PermissionException)
        {
            // Battery level is optional; never block the dashboard over it.
        }
    }

    private async void OnMenuClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(StatisticsPage));
}
