using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Trips;

public sealed class TripSummaryTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void From_RecordingTrip_UsesLastSampleSpeed()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(new LocationSample(41.0, 29.0, T0, 10, 5));

        var summary = TripSummary.From(trip, T0.AddMinutes(1));

        Assert.Equal(36, summary.CurrentSpeedKmh, 6);
        Assert.Equal(TimeSpan.FromMinutes(1), summary.Duration);
    }

    [Fact]
    public void From_CompletedTrip_HasZeroCurrentSpeed()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(new LocationSample(41.0, 29.0, T0, 10, 5));
        trip.Complete(T0.AddMinutes(1));

        Assert.Equal(0, TripSummary.From(trip, T0.AddHours(1)).CurrentSpeedKmh);
    }

    [Fact]
    public void Aggregate_SumsDistanceAndDuration()
    {
        var a = Trip.Start(T0);
        a.AddSample(new LocationSample(41.0, 29.0, T0, 10, 5));
        a.AddSample(new LocationSample(41.01, 29.0, T0.AddMinutes(30), 20, 5));
        a.Complete(T0.AddHours(1));
        var b = Trip.Start(T0.AddDays(1));
        b.Complete(T0.AddDays(1).AddHours(1));

        var stats = TripStatistics.Aggregate([a, b], T0.AddDays(2));

        Assert.Equal(2, stats.TripCount);
        Assert.Equal(TimeSpan.FromHours(2), stats.TotalDuration);
        Assert.Equal(a.DistanceKm, stats.TotalDistanceKm, 6);
        Assert.Equal(a.DistanceKm / 2, stats.AverageSpeedKmh, 6);
    }

    [Fact]
    public void Aggregate_Empty_IsZero()
    {
        var stats = TripStatistics.Aggregate([], T0);
        Assert.Equal(0, stats.TripCount);
        Assert.Equal(0, stats.AverageSpeedKmh);
    }
}
