using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IDashboardDataService _dashboardDataService;
    private int _speedKmh;
    private string _clock = "--:--";
    private string _gpsStatus = "Unavailable";
    private string _tripTime = "00:00";
    private double _distanceKm;
    private int _averageSpeedKmh;
    private int _maximumSpeedKmh;
    private bool _isBusy;

    public int SpeedKmh
    {
        get => _speedKmh;
        private set => SetProperty(ref _speedKmh, value);
    }

    public string Clock
    {
        get => _clock;
        private set => SetProperty(ref _clock, value);
    }

    public string GpsStatus
    {
        get => _gpsStatus;
        private set => SetProperty(ref _gpsStatus, value);
    }

    public string TripTime
    {
        get => _tripTime;
        private set => SetProperty(ref _tripTime, value);
    }

    public double DistanceKm
    {
        get => _distanceKm;
        private set => SetProperty(ref _distanceKm, value);
    }

    public int AverageSpeedKmh
    {
        get => _averageSpeedKmh;
        private set => SetProperty(ref _averageSpeedKmh, value);
    }

    public int MaximumSpeedKmh
    {
        get => _maximumSpeedKmh;
        private set => SetProperty(ref _maximumSpeedKmh, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public DashboardViewModel(IDashboardDataService dashboardDataService)
    {
        _dashboardDataService = dashboardDataService;
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
            Clock = snapshot.Clock;
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
