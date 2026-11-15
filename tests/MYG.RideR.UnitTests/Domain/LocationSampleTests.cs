using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Domain;

public sealed class LocationSampleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ValidAltitudeAndCourse_AreStored()
    {
        var sample = new LocationSample(41.0, 29.0, T0, altitudeMeters: 120.5, courseDegrees: 270);

        Assert.Equal(120.5, sample.AltitudeMeters);
        Assert.Equal(270, sample.CourseDegrees);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteAltitude_Throws(double altitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LocationSample(41.0, 29.0, T0, altitudeMeters: altitude));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(360.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CourseOutOfRangeOrNonFinite_Throws(double course)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new LocationSample(41.0, 29.0, T0, courseDegrees: course));
    }

    [Fact]
    public void Course360_IsNormalizedToZero()
    {
        var sample = new LocationSample(41.0, 29.0, T0, courseDegrees: 360);

        Assert.Equal(0, sample.CourseDegrees);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(359.9)]
    public void CourseInRange_IsKept(double course)
    {
        var sample = new LocationSample(41.0, 29.0, T0, courseDegrees: course);

        Assert.Equal(course, sample.CourseDegrees);
    }

    [Fact]
    public void AltitudeAndCourse_DefaultToNull()
    {
        var sample = new LocationSample(41.0, 29.0, T0);

        Assert.Null(sample.AltitudeMeters);
        Assert.Null(sample.CourseDegrees);
    }
}
