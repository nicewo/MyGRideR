using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Mobile.Services;

public sealed class MauiLocationService : ILocationService
{
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);
    private bool _listening;

    public event Action<LocationSample>? LocationChanged;

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listening)
            return true;

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
            return false;

        Geolocation.Default.LocationChanged += OnLocationChanged;
        var request = new GeolocationListeningRequest(GeolocationAccuracy.Best, MinimumInterval);

        _listening = await Geolocation.Default.StartListeningForegroundAsync(request);
        if (!_listening)
            Geolocation.Default.LocationChanged -= OnLocationChanged;

        return _listening;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_listening)
            return Task.CompletedTask;

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();
        _listening = false;
        return Task.CompletedTask;
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var l = e.Location;
        try
        {
            LocationChanged?.Invoke(new LocationSample(
                l.Latitude,
                l.Longitude,
                l.Timestamp,
                l.Speed is { } s and >= 0 ? s : null,
                l.Accuracy is { } a and >= 0 ? a : null));
        }
        catch (ArgumentOutOfRangeException)
        {
            // Invalid fix from the platform; skip it.
        }
    }
}
