using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Domain;

public sealed class TrackingFilterTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private const double MetersPerDegreeLat = 111_195;

    private static LocationSample At(double northMeters, int seconds, double? speed = null, double? accuracy = null) =>
        new(northMeters / MetersPerDegreeLat, 0, T0.AddSeconds(seconds), speed, accuracy);

    [Fact]
    public void Route_StationaryJitter_AccumulatesNoDistance()
    {
        var route = new Route();
        for (var i = 0; i < 60; i++)
            route.Add(At(i % 2 == 0 ? 8 : -8, i, accuracy: 20));

        Assert.Equal(0, route.DistanceMeters);
        Assert.Equal(60, route.Samples.Count);
    }

    [Fact]
    public void Route_StraightLine_AccumulatesExpectedDistance()
    {
        var route = new Route();
        for (var i = 0; i <= 60; i++)
            route.Add(At(i * 10.0, i));

        Assert.InRange(route.DistanceMeters, 600 * 0.98, 600 * 1.02);
    }

    [Fact]
    public void Route_PoorAccuracySample_IsIgnoredForDistance()
    {
        var route = new Route();
        route.Add(At(0, 0));
        route.Add(At(500, 60, accuracy: 50));

        Assert.Equal(0, route.DistanceMeters);
        Assert.Equal(2, route.Samples.Count);
    }

    [Fact]
    public void Route_SingleJump_DoesNotChangeDistance()
    {
        var route = new Route();
        route.Add(At(0, 0));
        route.Add(At(2000, 1));

        Assert.Equal(0, route.DistanceMeters);
    }

    [Fact]
    public void Route_ThreeConsecutiveJumps_ReanchorWithoutAddingDistance()
    {
        var route = new Route();
        route.Add(At(0, 0));
        route.Add(At(2000, 1));
        route.Add(At(2010, 2));
        route.Add(At(2020, 3));

        Assert.Equal(0, route.DistanceMeters);

        // Çapa artık 2020 m'de: normal hareket eklenir.
        route.Add(At(2120, 13));
        Assert.InRange(route.DistanceMeters, 99, 101);
    }

    [Fact]
    public void Trip_SingleSpeedSpike_DoesNotChangeMaximum()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(At(0, 0, speed: 10));
        trip.AddSample(At(10, 1, speed: 60));
        trip.AddSample(At(20, 2, speed: 10));
        trip.AddSample(At(30, 3, speed: 10));

        Assert.Equal(36, trip.MaximumSpeedKmh, 6);
    }

    [Fact]
    public void Trip_ThreeConsecutiveSpeeds_SetMaximum()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(At(0, 0, speed: 20));
        trip.AddSample(At(20, 1, speed: 20));
        trip.AddSample(At(40, 2, speed: 20));

        Assert.Equal(72, trip.MaximumSpeedKmh, 6);
    }
}
