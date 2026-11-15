namespace MYG.RideR.Application.Abstractions;

public interface IRecordingKeepAlive
{
    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);
}
