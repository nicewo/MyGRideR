namespace MYG.RideR.Mobile.Interfaces;

/// <summary>Satellite counts from the platform GNSS engine (MAUI Geolocation does not expose them).</summary>
public interface IGnssStatusService
{
    /// <summary>(usedInFix, visible). Raised on the main thread.</summary>
    event Action<int, int>? StatusChanged;

    /// <summary>Starts listening; requires location permission to be granted already.</summary>
    void Start();

    void Stop();
}
