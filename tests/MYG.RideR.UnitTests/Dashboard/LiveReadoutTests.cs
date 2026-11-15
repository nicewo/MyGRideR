using MYG.RideR.Application.Dashboard;

namespace MYG.RideR.UnitTests.Dashboard;

public sealed class LiveReadoutTests
{
    [Theory]
    [InlineData(null, "--")]
    [InlineData(double.NaN, "--")]
    [InlineData(0.0, "K 0°")]
    [InlineData(22.4, "K 22°")]
    [InlineData(22.5, "KD 23°")]
    [InlineData(45.0, "KD 45°")]
    [InlineData(90.0, "D 90°")]
    [InlineData(180.0, "G 180°")]
    [InlineData(337.5, "K 338°")]
    [InlineData(359.9, "K 0°")]
    [InlineData(360.0, "K 0°")]
    [InlineData(-10.0, "K 350°")]
    public void HeadingText_FormatsSectorAndDegrees(double? course, string expected)
    {
        Assert.Equal(expected, LiveReadout.HeadingText(course));
    }

    [Theory]
    [InlineData(null, "±--")]
    [InlineData(0.0, "±0m")]
    [InlineData(4.6, "±5m")]
    [InlineData(12.0, "±12m")]
    public void AccuracyText_FormatsRoundedMeters(double? accuracy, string expected)
    {
        Assert.Equal(expected, LiveReadout.AccuracyText(accuracy));
    }

    [Theory]
    [InlineData(null, GpsQuality.NoFix)]
    [InlineData(0.0, GpsQuality.Good)]
    [InlineData(10.0, GpsQuality.Good)]
    [InlineData(10.1, GpsQuality.Fair)]
    [InlineData(25.0, GpsQuality.Fair)]
    [InlineData(25.1, GpsQuality.Poor)]
    [InlineData(100.0, GpsQuality.Poor)]
    public void GpsQuality_UsesAccuracyThresholds(double? accuracy, GpsQuality expected)
    {
        Assert.Equal(expected, LiveReadout.GpsQuality(accuracy));
    }
}
