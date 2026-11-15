using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.Services;

public sealed class NoOpAppUpdater : IAppUpdater
{
    public Task CheckAndInstallAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
