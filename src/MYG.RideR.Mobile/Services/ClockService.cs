using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public class ClockService : IClockService
{
    private Timer? _timer;

    public event Action<DateTime>? TimeChanged;

    public void Start()
    {
        if (_timer != null)
            return;

        _timer = new Timer(
            _ => TimeChanged?.Invoke(DateTime.Now),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
