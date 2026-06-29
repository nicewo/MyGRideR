using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public class ClockService : IClockService
{
    private IDispatcherTimer? _timer;

    public event Action<DateTime>? TimeChanged;

    public void Start()
    {
        if (_timer != null)
            return;

        _timer = Microsoft.Maui.Controls.Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);

        _timer.Tick += (s, e) =>
        {
            TimeChanged?.Invoke(DateTime.Now);
        };

        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }
}
