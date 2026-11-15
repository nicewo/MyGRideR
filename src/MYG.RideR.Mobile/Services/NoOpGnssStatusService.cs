using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public sealed class NoOpGnssStatusService : IGnssStatusService
{
    public event Action<int, int>? StatusChanged
    {
        add { }
        remove { }
    }

    public void Start()
    {
    }

    public void Stop()
    {
    }
}
