using MYG.RideR.Mobile.Interfaces;
using MYG.RideR.Mobile.ViewModels;

namespace MYG.RideR.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly IErrorReporter _errorReporter;
    private static bool _reportPrompted;
    private IDispatcherTimer? _routeTimer;

    public DashboardPage(DashboardViewModel viewModel, IErrorReporter errorReporter)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _errorReporter = errorReporter;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartClock();
        _viewModel.PositionChanged -= ViewModel_PositionChanged;
        _viewModel.PositionChanged += ViewModel_PositionChanged;
        StartRouteTimer();

        try
        {
            await _viewModel.InitializeAsync();
            await _viewModel.StartLiveAsync();
            await OfferErrorReportAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard initialization failed: {ex}");
        }
    }

    private void ViewModel_PositionChanged(double lat, double lon, double? course) =>
        MapView.UpdatePosition(lat, lon, course);

    private void StartRouteTimer()
    {
        if (_routeTimer is null)
        {
            _routeTimer = Dispatcher.CreateTimer();
            _routeTimer.Interval = TimeSpan.FromSeconds(5);
            _routeTimer.Tick += (_, _) =>
                MapView.SetRoute(_viewModel.IsRecording ? _viewModel.GetCurrentRoutePoints() : []);
        }

        _routeTimer.Start();
    }

    private async Task OfferErrorReportAsync()
    {
        if (_reportPrompted || !await _errorReporter.HasUnreportedAsync())
            return;

        _reportPrompted = true;
        if (await DisplayAlertAsync("Hata kaydı", "Uygulamada hata kaydedildi. Rapor e-postayla gönderilsin mi?", "Gönder", "Sonra"))
            await _errorReporter.SendAsync();
    }

    protected override async void OnDisappearing()
    {
        _viewModel.StopClock();
        _viewModel.PositionChanged -= ViewModel_PositionChanged;
        _routeTimer?.Stop();
        base.OnDisappearing();

        try
        {
            await _viewModel.StopLiveAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard live feed release failed: {ex}");
        }
    }
}
