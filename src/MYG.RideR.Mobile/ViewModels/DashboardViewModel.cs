using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataService _dashboardDataService;
    private readonly IClockService _clockService;

    [ObservableProperty]
    public partial int SpeedKmh { get; private set; }

    [ObservableProperty]
    public partial string Clock { get; private set; } = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    public partial string GpsStatus { get; private set; } = "Unavailable";

    [ObservableProperty]
    public partial string TripTime { get; private set; } = "00:00";

    [ObservableProperty]
    public partial double DistanceKm { get; private set; }

    [ObservableProperty]
    public partial int AverageSpeedKmh { get; private set; }

    [ObservableProperty]
    public partial int MaximumSpeedKmh { get; private set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    public DashboardViewModel(
        IDashboardDataService dashboardDataService,
        IClockService clockService)
    {
        _dashboardDataService = dashboardDataService;
        _clockService = clockService;
    }

    public void StartClock()
    {
        _clockService.TimeChanged -= ClockService_TimeChanged;
        _clockService.TimeChanged += ClockService_TimeChanged;
        _clockService.Start();
    }

    public void StopClock()
    {
        _clockService.TimeChanged -= ClockService_TimeChanged;
        _clockService.Stop();
    }

    private void ClockService_TimeChanged(DateTime now)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Clock = now.ToString("HH:mm:ss");
        });
    }

    [RelayCommand]
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            var snapshot = await _dashboardDataService.GetSnapshotAsync(cancellationToken);

            SpeedKmh = snapshot.SpeedKmh;
            GpsStatus = snapshot.GpsStatus;
            TripTime = snapshot.TripTime;
            DistanceKm = snapshot.DistanceKm;
            AverageSpeedKmh = snapshot.AverageSpeedKmh;
            MaximumSpeedKmh = snapshot.MaximumSpeedKmh;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
