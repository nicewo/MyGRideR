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

    [Theory]
    [InlineData(39.426139, 29.995139, "39°25'34.1\"N, 29°59'42.5\"E")]
    [InlineData(-33.5, -70.25, "33°30'00.0\"S, 70°15'00.0\"W")]
    [InlineData(0, 0, "0°00'00.0\"N, 0°00'00.0\"E")]
    [InlineData(10.99999999, 20, "11°00'00.0\"N, 20°00'00.0\"E")]
    public void CoordinateText_FormatsDegreesMinutesSeconds(double lat, double lon, string expected)
    {
        Assert.Equal(expected, LiveReadout.CoordinateText(lat, lon));
    }

    [Theory]
    [InlineData(26.4, "000026")]
    [InlineData(0, "000000")]
    [InlineData(-5, "000000")]
    [InlineData(999999.9, "999999")]
    [InlineData(1234567, "1234567")]
    [InlineData(double.NaN, "000000")]
    public void OdometerText_ZeroPadsWholeKilometres(double km, string expected)
    {
        Assert.Equal(expected, LiveReadout.OdometerText(km));
    }
}
