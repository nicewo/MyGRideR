namespace MYG.RideR.Mobile.Controls;

public partial class SpeedGauge : ContentView
{
    public static readonly BindableProperty SpeedKmhProperty = BindableProperty.Create(
        nameof(SpeedKmh), typeof(int), typeof(SpeedGauge), 0,
        propertyChanged: (b, _, n) => ((SpeedGauge)b).OnSpeedChanged((int)n));

    private readonly GaugeDrawable _drawable = new();

    public int SpeedKmh
    {
        get => (int)GetValue(SpeedKmhProperty);
        set => SetValue(SpeedKmhProperty, value);
    }

    public SpeedGauge()
    {
        InitializeComponent();
        Canvas.Drawable = _drawable;
    }

    private void OnSpeedChanged(int speed)
    {
        SpeedLabel.Text = speed.ToString();
        _drawable.Speed = speed;
        Canvas.Invalidate();
    }
}
