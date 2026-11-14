using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed record TripSummary(
    double CurrentSpeedKmh,
    double DistanceKm,
    double AverageSpeedKmh,
    double MaximumSpeedKmh,
    TimeSpan Duration)
{
    public static TripSummary Empty { get; } = new(0, 0, 0, 0, TimeSpan.Zero);

    public static TripSummary From(Trip trip, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var samples = trip.Route.Samples;
        var speedMps = samples.Count > 0 ? samples[^1].SpeedMetersPerSecond ?? 0 : 0;
        var current = trip.Status == TripStatus.Recording ? speedMps * 3.6 : 0;

        return new TripSummary(
            current,
            trip.DistanceKm,
            trip.AverageSpeedKmh(now),
            trip.MaximumSpeedKmh,
            trip.Duration(now));
    }
}
