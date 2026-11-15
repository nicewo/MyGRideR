using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Abstractions;

public interface ILiveLocationFeed
{
    LocationSample? LatestSample { get; }

    event Action<LocationSample>? SampleReceived;

    Task<bool> AcquireAsync(CancellationToken cancellationToken = default);

    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
