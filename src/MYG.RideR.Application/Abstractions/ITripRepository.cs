using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ITripRepository
{
    Task SaveAsync(Trip trip, CancellationToken cancellationToken = default);

    Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes the trip header row (EndedAt NULL) when a trip starts.</summary>
    Task CreateAsync(Guid id, DateTimeOffset startedAt, CancellationToken cancellationToken = default);

    /// <summary>Appends samples in one transaction; <paramref name="firstSeq"/> is the Seq of the first sample.</summary>
    Task AppendSamplesAsync(Guid tripId, int firstSeq, IReadOnlyList<LocationSample> samples, CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid tripId, DateTimeOffset endedAt, CancellationToken cancellationToken = default);

    /// <summary>Trips with EndedAt IS NULL.</summary>
    Task<IReadOnlyList<Trip>> ListUnfinishedAsync(CancellationToken cancellationToken = default);
}
