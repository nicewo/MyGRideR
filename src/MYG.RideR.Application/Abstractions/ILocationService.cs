using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ILocationService
{
    event Action<LocationSample>? LocationChanged;

    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
