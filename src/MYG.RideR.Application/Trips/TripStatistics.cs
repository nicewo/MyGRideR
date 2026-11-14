using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed record TripStatistics(
    int TripCount,
    double TotalDistanceKm,
    TimeSpan TotalDuration,
    double AverageSpeedKmh,
    double MaximumSpeedKmh)
{
    public static TripStatistics Aggregate(IEnumerable<Trip> trips, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(trips);

        var count = 0;
        var distance = 0.0;
        var duration = TimeSpan.Zero;
        var max = 0.0;

        foreach (var trip in trips)
        {
            count++;
            distance += trip.DistanceKm;
            duration += trip.Duration(now);
            max = Math.Max(max, trip.MaximumSpeedKmh);
        }

        var average = duration.TotalHours > 0 ? distance / duration.TotalHours : 0;
        return new TripStatistics(count, distance, duration, average, max);
    }
}
