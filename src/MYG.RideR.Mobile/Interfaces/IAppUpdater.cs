namespace MYG.RideR.Mobile.Interfaces;

public interface IAppUpdater
{
    /// <summary>Checks GitHub for a newer build; downloads and opens the installer if found. Never throws.</summary>
    Task CheckAndInstallAsync(CancellationToken cancellationToken = default);
}
