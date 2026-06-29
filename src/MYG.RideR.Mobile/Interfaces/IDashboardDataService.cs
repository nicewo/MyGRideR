using MYG.RideR.Mobile.Models;

namespace MYG.RideR.Mobile.Interfaces;

public interface IDashboardDataService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
