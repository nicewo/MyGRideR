using MYG.RideR.Mobile.Interfaces;
using MYG.RideR.Mobile.Models;

namespace MYG.RideR.Mobile.Services;

public sealed class DashboardMockDataService : IDashboardDataService
{
    public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = new DashboardSnapshot(
            SpeedKmh: 72,
            Clock: DateTime.Now.ToString("HH:mm"),
            GpsStatus: "Mock Ready",
            TripTime: "00:42",
            DistanceKm: 18.6,
            AverageSpeedKmh: 54,
            MaximumSpeedKmh: 96);

        return Task.FromResult(snapshot);
    }
}
