using System.Globalization;

namespace MYG.RideR.Application.Dashboard;

public enum GpsQuality
{
    NoFix,
    Poor,
    Fair,
    Good
}

public static class LiveReadout
{
    public const double GoodAccuracyMeters = 10;
    public const double FairAccuracyMeters = 25;

    private static readonly string[] Sectors = ["K", "KD", "D", "GD", "G", "GB", "B", "KB"];

    public static string HeadingText(double? course)
    {
        if (course is not { } c || !double.IsFinite(c))
            return "--";

        var normalized = ((c % 360) + 360) % 360;
        var sector = (int)((normalized + 22.5) / 45) % 8;
        var degrees = (int)Math.Round(normalized, MidpointRounding.AwayFromZero) % 360;
        return string.Create(CultureInfo.InvariantCulture, $"{Sectors[sector]} {degrees}°");
    }

    public static string AccuracyText(double? accuracyMeters) =>
        accuracyMeters is { } a
            ? string.Create(CultureInfo.InvariantCulture, $"±{Math.Round(a):0}m")
            : "±--";

    public static GpsQuality GpsQuality(double? accuracyMeters) => accuracyMeters switch
    {
        null => Dashboard.GpsQuality.NoFix,
        <= GoodAccuracyMeters => Dashboard.GpsQuality.Good,
        <= FairAccuracyMeters => Dashboard.GpsQuality.Fair,
        _ => Dashboard.GpsQuality.Poor
    };
}
