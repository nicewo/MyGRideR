using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Dashboard;
using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ITripRecorder _tripRecorder;
    private readonly IClockService _clockService;
    private readonly ILiveLocationFeed _liveFeed;
    private readonly TimeProvider _timeProvider;
    private readonly ICrashLog _crashLog;
    private bool _liveAcquired;
    private double? _lastCourse;
    private readonly SemaphoreSlim _liveGate = new(1, 1);
    private bool _recovered;
    private string? _recordingError;
    private DateTimeOffset _recordingErrorUntil;

    [ObservableProperty]
    public partial int SpeedKmh { get; private set; }

    [ObservableProperty]
    public partial string Clock { get; private set; } = DateTime.Now.ToString("HH:mm:ss");

    [ObservableProperty]
    public partial string ClockShort { get; private set; } = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    public partial string DateText { get; private set; } = FormatDate(DateTime.Now);

    [ObservableProperty]
    public partial string GpsStatus { get; private set; } = "GPS yok";

    [ObservableProperty]
    public partial string Gear { get; private set; } = "-";

    [ObservableProperty]
    public partial int AltitudeM { get; private set; }

    [ObservableProperty]
    public partial string StopTime { get; private set; } = "00:00";

    [ObservableProperty]
    public partial string HeadingText { get; private set; } = "--";

    [ObservableProperty]
    public partial string AccuracyText { get; private set; } = "±--";

    [ObservableProperty]
    public partial string SatelliteText { get; private set; } = "0/0";

    [ObservableProperty]
    public partial string BatteryText { get; private set; } = "%--";

    [ObservableProperty]
    public partial double FuelRatio { get; private set; } = 0.5;

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
    public partial string RecordButtonText { get; private set; } = "BAŞLAT";

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    public DashboardViewModel(
        ITripRecorder tripRecorder,
        IClockService clockService,
        ILiveLocationFeed liveFeed,
        TimeProvider timeProvider,
        ICrashLog crashLog)
    {
        _crashLog = crashLog;
        _liveFeed = liveFeed;
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
            ClockShort = now.ToString("HH:mm");
            DateText = FormatDate(now);
            Refresh();
        });
    }

    private static string FormatDate(DateTime d) =>
        d.ToString("d MMMM yyyy dddd", new System.Globalization.CultureInfo("tr-TR"));

    [RelayCommand]
    public async Task InitializeAsync()
    {
        Refresh();

        if (_recovered)
            return;
        _recovered = true;

        try
        {
            if (await _tripRecorder.RecoverUnfinishedAsync() > 0)
                ShowRecordingError("Yarım kalan sürüş kaydedildi");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            _crashLog.Record(ex, "Recover unfinished trips");
        }
    }

    [RelayCommand]
    public async Task ToggleRecordingAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var starting = _tripRecorder.CurrentTrip is null;
        try
        {
            IsBusy = true;
            _recordingError = null;
            if (starting)
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
            _crashLog.Record(ex, starting ? "Recording start" : "Recording stop");
            ShowRecordingError(ex.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            _crashLog.Record(ex, starting ? "Recording start" : "Recording stop");
            ShowRecordingError(starting
                ? $"Kayıt başlatılamadı: {ex.Message}"
                : "Kayıt kaydedilemedi, tekrar DURDUR'a bas");
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    private void ShowRecordingError(string message)
    {
        _recordingError = message;
        _recordingErrorUntil = _timeProvider.GetUtcNow().AddSeconds(5);
        GpsStatus = message;
    }

    private void Refresh()
    {
        var trip = _tripRecorder.CurrentTrip;
        IsRecording = trip is not null;
        RecordButtonText = trip?.Status == TripStatus.Completed ? "KAYDET" : IsRecording ? "DURDUR" : "BAŞLAT";

        if (trip is null)
        {
            var latestSpeed = _liveFeed.LatestSample?.SpeedMetersPerSecond;
            SpeedKmh = latestSpeed is { } mps ? (int)Math.Round(mps * 3.6) : 0;
            return;
        }

        var summary = _tripRecorder.GetCurrentSummary(_timeProvider.GetUtcNow());
        if (summary is null)
            return;

        SpeedKmh = (int)Math.Round(summary.CurrentSpeedKmh);
        DistanceKm = Math.Round(summary.DistanceKm, 2);
        AverageSpeedKmh = (int)Math.Round(summary.AverageSpeedKmh);
        MaximumSpeedKmh = (int)Math.Round(summary.MaximumSpeedKmh);
        TripTime = FormatSpan(summary.Duration);
        StopTime = FormatSpan(summary.StoppedDuration);
    }

    private static string FormatSpan(TimeSpan span) =>
        span.ToString(span.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");

    public async Task StartLiveAsync()
    {
        await _liveGate.WaitAsync();
        try
        {
            if (_liveAcquired)
            {
                return;
            }

            _liveFeed.SampleReceived += LiveFeed_SampleReceived;
            bool acquired;
            try
            {
                acquired = await _liveFeed.AcquireAsync();
            }
            catch (Exception)
            {
                acquired = false;
            }

            if (acquired)
            {
                _liveAcquired = true;
                if (_liveFeed.LatestSample is { } latest)
                {
                    ApplyLive(latest);
                }
            }
            else
            {
                _liveFeed.SampleReceived -= LiveFeed_SampleReceived;
                ApplyQuality(GpsQuality.NoFix);
            }
        }
        finally
        {
            _liveGate.Release();
        }
    }

    public async Task StopLiveAsync()
    {
        await _liveGate.WaitAsync();
        try
        {
            if (!_liveAcquired)
            {
                return;
            }

            _liveAcquired = false;
            _liveFeed.SampleReceived -= LiveFeed_SampleReceived;
            await _liveFeed.ReleaseAsync();
        }
        finally
        {
            _liveGate.Release();
        }
    }

    private void LiveFeed_SampleReceived(LocationSample sample)
    {
        MainThread.BeginInvokeOnMainThread(() => ApplyLive(sample));
    }

    private void ApplyLive(LocationSample sample)
    {
        if (sample.AltitudeMeters is { } altitude)
        {
            AltitudeM = (int)Math.Round(altitude);
        }

        var moving = sample.SpeedMetersPerSecond is not { } mps || mps * 3.6 >= TripSummary.StopSpeedThresholdKmh;
        if (moving && sample.CourseDegrees is { } course)
        {
            _lastCourse = course;
        }

        HeadingText = LiveReadout.HeadingText(_lastCourse);
        AccuracyText = LiveReadout.AccuracyText(sample.AccuracyMeters);
        ApplyQuality(LiveReadout.GpsQuality(sample.AccuracyMeters));
        Refresh();
    }

    private void ApplyQuality(GpsQuality quality)
    {
        if (_recordingError is not null && _timeProvider.GetUtcNow() < _recordingErrorUntil)
        {
            GpsStatus = _recordingError;
        }
        else
        {
            _recordingError = null;
            GpsStatus = quality switch
            {
                GpsQuality.Good => "GPS iyi",
                GpsQuality.Fair or GpsQuality.Poor => "GPS zayıf",
                _ => "GPS yok"
            };
        }

        if (quality == GpsQuality.NoFix)
        {
            AccuracyText = LiveReadout.AccuracyText(null);
        }
    }
}
