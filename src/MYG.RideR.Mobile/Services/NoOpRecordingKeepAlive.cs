using MYG.RideR.Application.Abstractions;

namespace MYG.RideR.Mobile.Services;

public sealed class NoOpRecordingKeepAlive : IRecordingKeepAlive
{
    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
}
