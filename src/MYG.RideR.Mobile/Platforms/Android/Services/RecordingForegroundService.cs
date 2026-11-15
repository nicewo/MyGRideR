using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace MYG.RideR.Mobile.Platforms.Android.Services;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public sealed class RecordingForegroundService : Service
{
    private const string ChannelId = "ride_recording";
    private const int NotificationId = 1001;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        EnsureChannel();

        var openIntent = new Intent(this, typeof(MainActivity));
        openIntent.SetFlags(ActivityFlags.SingleTop);
        var pending = PendingIntent.GetActivity(
            this, 0, openIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("MYG RideR")!
            .SetContentText("Sürüş kaydediliyor")!
            .SetOngoing(true)!
            .SetSmallIcon(Resource.Mipmap.appicon)!
            .SetContentIntent(pending)!
            .Build()!;

        ServiceCompat.StartForeground(this, NotificationId, notification, (int)ForegroundService.TypeLocation);
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null)
            return;

        manager.CreateNotificationChannel(
            new NotificationChannel(ChannelId, "Sürüş kaydı", NotificationImportance.Low));
    }
}
