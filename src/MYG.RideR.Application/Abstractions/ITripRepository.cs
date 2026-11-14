using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ITripRepository
{
    Task SaveAsync(Trip trip, CancellationToken cancellationToken = default);

    Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default);
}
