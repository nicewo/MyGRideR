using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Location;

public sealed class LiveLocationFeed : ILiveLocationFeed
{
    private readonly ILocationService _locationService;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private int _holders;
    private LocationSample? _latest;

    public LiveLocationFeed(ILocationService locationService)
    {
        _locationService = locationService;
    }

    public LocationSample? LatestSample
    {
        get
        {
            lock (_stateLock)
                return _latest;
        }
    }

    public event Action<LocationSample>? SampleReceived;

    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_holders == 0)
            {
                _locationService.LocationChanged += OnLocationChanged;
                bool started;
                try
                {
                    started = await _locationService.StartAsync(cancellationToken);
                }
                catch
                {
                    _locationService.LocationChanged -= OnLocationChanged;
                    throw;
                }

                if (!started)
                {
                    _locationService.LocationChanged -= OnLocationChanged;
                    return false;
                }
            }

            _holders++;
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_holders == 0)
                return;

            _holders--;
            if (_holders > 0)
                return;

            _locationService.LocationChanged -= OnLocationChanged;
            await _locationService.StopAsync(cancellationToken);
            lock (_stateLock)
                _latest = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnLocationChanged(LocationSample sample)
    {
        lock (_stateLock)
            _latest = sample;

        SampleReceived?.Invoke(sample);
    }
}
