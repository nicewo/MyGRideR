namespace MYG.RideR.Mobile.Controls;

public partial class StatCard : ContentView
{
    public StatCard()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(
            nameof(Title),
            typeof(string),
            typeof(StatCard),
            string.Empty);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(object),
            typeof(StatCard),
            null);

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(
            nameof(Unit),
            typeof(string),
            typeof(StatCard),
            string.Empty);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }
}
