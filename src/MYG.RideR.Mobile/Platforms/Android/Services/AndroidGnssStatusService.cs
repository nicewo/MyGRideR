using Android.Content;
using Android.Locations;
using Android.OS;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Platforms.Android.Services;

public sealed class AndroidGnssStatusService : IGnssStatusService
{
    private Callback? _callback;

    public event Action<int, int>? StatusChanged;

    public void Start()
    {
        if (_callback is not null || Build.VERSION.SdkInt < BuildVersionCodes.N)
            return;

        try
        {
            var manager = (LocationManager?)Platform.AppContext.GetSystemService(Context.LocationService);
            if (manager is null)
                return;

            var callback = new Callback(this);
            if (manager.RegisterGnssStatusCallback(callback, new Handler(Looper.MainLooper!)))
                _callback = callback;
        }
        catch (Java.Lang.SecurityException ex)
        {
            // Location permission not granted yet; the next Start (after the live feed is acquired) retries.
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    public void Stop()
    {
        if (_callback is null)
            return;

        try
        {
            var manager = (LocationManager?)Platform.AppContext.GetSystemService(Context.LocationService);
            manager?.UnregisterGnssStatusCallback(_callback);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        _callback = null;
    }

    private void Raise(int used, int visible) => StatusChanged?.Invoke(used, visible);

    private sealed class Callback : GnssStatus.Callback
    {
        private readonly AndroidGnssStatusService _owner;

        public Callback(AndroidGnssStatusService owner)
        {
            _owner = owner;
        }

        public override void OnSatelliteStatusChanged(GnssStatus status)
        {
            var visible = status.SatelliteCount;
            var used = 0;
            for (var i = 0; i < visible; i++)
            {
                if (status.UsedInFix(i))
                    used++;
            }

            _owner.Raise(used, visible);
        }

        public override void OnStopped() => _owner.Raise(0, 0);
    }
}
