using Android.Content;
using Android.OS;
using AndroidX.Core.Content;
using MYG.RideR.Application.Abstractions;

namespace MYG.RideR.Mobile.Platforms.Android.Services;

public sealed class AndroidRecordingKeepAlive : IRecordingKeepAlive
{
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            // Denial is fine: the service still runs, only its notification is hidden.
            await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.PostNotifications>());
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
            ContextCompat.StartForegroundService(
                Platform.AppContext,
                new Intent(Platform.AppContext, typeof(RecordingForegroundService))));
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        Platform.AppContext.StopService(new Intent(Platform.AppContext, typeof(RecordingForegroundService)));
        return Task.CompletedTask;
    }
}
