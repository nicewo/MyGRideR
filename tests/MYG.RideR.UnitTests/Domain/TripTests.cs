using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Domain;

public sealed class TripTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void LocationSample_RejectsInvalidCoordinates(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LocationSample(lat, lon, T0));
    }

    [Fact]
    public void DistanceMetersTo_OneDegreeOfLatitude_IsAbout111Km()
    {
        var a = new LocationSample(0, 0, T0);
        var b = new LocationSample(1, 0, T0);

        Assert.InRange(a.DistanceMetersTo(b), 111_000, 111_400);
    }

    [Fact]
    public void Route_AccumulatesDistance()
    {
        var route = new Route();
        route.Add(new LocationSample(0, 0, T0));
        route.Add(new LocationSample(0.01, 0, T0.AddMinutes(1)));
        route.Add(new LocationSample(0.02, 0, T0.AddMinutes(2)));

        Assert.InRange(route.DistanceMeters, 2_200, 2_240);
    }

    [Fact]
    public void Route_RejectsOutOfOrderSamples()
    {
        var route = new Route();
        route.Add(new LocationSample(0, 0, T0));

        Assert.Throws<ArgumentException>(() => route.Add(new LocationSample(0, 0, T0.AddSeconds(-1))));
    }

    [Fact]
    public void Trip_TracksMaximumSpeedAndAverage()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(new LocationSample(0, 0, T0, speedMetersPerSecond: 10));
        trip.AddSample(new LocationSample(0.09, 0, T0.AddHours(1), speedMetersPerSecond: 25));

        Assert.Equal(90, trip.MaximumSpeedKmh, 6);
        Assert.InRange(trip.AverageSpeedKmh(T0.AddHours(1)), 9.9, 10.1);
    }

    [Fact]
    public void Trip_AverageSpeed_IsZeroBeforeAnyTimePasses()
    {
        var trip = Trip.Start(T0);

        Assert.Equal(0, trip.AverageSpeedKmh(T0));
    }

    [Fact]
    public void Trip_Complete_FreezesDurationAndBlocksSamples()
    {
        var trip = Trip.Start(T0);
        trip.Complete(T0.AddMinutes(30));

        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(TimeSpan.FromMinutes(30), trip.Duration(T0.AddHours(5)));
        Assert.Throws<InvalidOperationException>(() => trip.AddSample(new LocationSample(0, 0, T0.AddMinutes(31))));
        Assert.Throws<InvalidOperationException>(() => trip.Complete(T0.AddMinutes(40)));
    }

    [Fact]
    public void Trip_Complete_RejectsEndBeforeStart()
    {
        var trip = Trip.Start(T0);

        Assert.Throws<ArgumentOutOfRangeException>(() => trip.Complete(T0.AddSeconds(-1)));
    }
}
