namespace MYG.RideR.Application.Trips;

public sealed record TripHeader(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    double DistanceKm,
    double MaximumSpeedKmh,
    TimeSpan StoppedDuration)
{
    public TimeSpan Duration(DateTimeOffset now) => (EndedAt ?? now) - StartedAt;
}
