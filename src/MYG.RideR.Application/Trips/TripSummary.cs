using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed record TripSummary(
    double CurrentSpeedKmh,
    double DistanceKm,
    double AverageSpeedKmh,
    double MaximumSpeedKmh,
    TimeSpan Duration,
    TimeSpan StoppedDuration)
{
    public const double StopSpeedThresholdKmh = 3;

    public static TripSummary Empty { get; } = new(0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero);

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
            trip.Duration(now),
            StoppedDurationOf(trip, now));
    }

    private static TimeSpan StoppedDurationOf(Trip trip, DateTimeOffset now)
    {
        var samples = trip.Route.Samples;
        var stopped = TimeSpan.Zero;
        var end = trip.EndedAt ?? now;

        for (var i = 0; i < samples.Count; i++)
        {
            if (!IsStopped(samples[i]))
                continue;

            if (i + 1 < samples.Count)
                stopped += samples[i + 1].Timestamp - samples[i].Timestamp;
            else if (end > samples[i].Timestamp)
                stopped += end - samples[i].Timestamp;
        }

        return stopped;
    }

    private static bool IsStopped(LocationSample sample) =>
        sample.SpeedMetersPerSecond is { } speed && speed * 3.6 < StopSpeedThresholdKmh;
}
