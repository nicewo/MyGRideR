using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ITripRecorder _tripRecorder;
    private readonly IClockService _clockService;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty]
    public partial int SpeedKmh { get; private set; }

    [ObservableProperty]
    public partial string Clock { get; private set; } = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    public partial string GpsStatus { get; private set; } = "GPS hazır değil";

    [ObservableProperty]
    public partial string TripTime { get; private set; } = "00:00";

    [ObservableProperty]
    public partial double DistanceKm { get; private set; }

    [ObservableProperty]
    public partial int AverageSpeedKmh { get; private set; }

    [ObservableProperty]
    public partial int MaximumSpeedKmh { get; private set; }

    [ObservableProperty]
    public partial bool IsRecording { get; private set; }

    [ObservableProperty]
    public partial string RecordButtonText { get; private set; } = "BAŞLA";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    public DashboardViewModel(
        ITripRecorder tripRecorder,
        IClockService clockService,
        TimeProvider timeProvider)
    {
        _tripRecorder = tripRecorder;
        _clockService = clockService;
        _timeProvider = timeProvider;
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
            Refresh();
        });
    }

    [RelayCommand]
    public Task InitializeAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task ToggleRecordingAsync()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            if (_tripRecorder.CurrentTrip is null)
            {
                await _tripRecorder.StartAsync();
            }
            else
            {
                await _tripRecorder.StopAsync();
            }
        }
        catch (InvalidOperationException ex)
        {
            GpsStatus = ex.Message;
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    private void Refresh()
    {
        var trip = _tripRecorder.CurrentTrip;
        IsRecording = trip is not null;
        RecordButtonText = IsRecording ? "DURDUR" : "BAŞLA";

        if (trip is null)
        {
            if (GpsStatus == "Kayıt açık")
            {
                GpsStatus = "GPS hazır değil";
            }

            return;
        }

        var summary = TripSummary.From(trip, _timeProvider.GetUtcNow());
        GpsStatus = "Kayıt açık";
        SpeedKmh = (int)Math.Round(summary.CurrentSpeedKmh);
        DistanceKm = Math.Round(summary.DistanceKm, 2);
        AverageSpeedKmh = (int)Math.Round(summary.AverageSpeedKmh);
        MaximumSpeedKmh = (int)Math.Round(summary.MaximumSpeedKmh);
        TripTime = summary.Duration.ToString(summary.Duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
    }
}
