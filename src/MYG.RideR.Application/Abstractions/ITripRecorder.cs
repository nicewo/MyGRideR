using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ITripRecorder
{
    Trip? CurrentTrip { get; }

    /// <summary>Thread-safe snapshot of the current trip; null when there is no trip.</summary>
    TripSummary? GetCurrentSummary(DateTimeOffset now);

    /// <summary>Thread-safe copy of the current trip's route points; empty when there is no trip.</summary>
    IReadOnlyList<(double Lat, double Lon)> GetCurrentRoutePoints();

    Task<Trip> StartAsync(CancellationToken cancellationToken = default);

    Task<Trip> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes trips left open by a crash; returns how many were closed.</summary>
    Task<int> RecoverUnfinishedAsync(CancellationToken cancellationToken = default);
}
