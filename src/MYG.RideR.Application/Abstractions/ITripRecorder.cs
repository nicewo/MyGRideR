using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ITripRecorder
{
    Trip? CurrentTrip { get; }

    Task<Trip> StartAsync(CancellationToken cancellationToken = default);

    Task<Trip> StopAsync(CancellationToken cancellationToken = default);
}
