using MYG.RideR.Mobile.Interfaces;
using MYG.RideR.Mobile.ViewModels;

namespace MYG.RideR.Mobile.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;
    private readonly IErrorReporter _errorReporter;
    private static bool _reportPrompted;

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
