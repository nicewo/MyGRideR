namespace MYG.RideR.Core.Domain;

public sealed record LocationSample
{
    public LocationSample(
        double latitude,
        double longitude,
        DateTimeOffset timestamp,
        double? speedMetersPerSecond = null,
        double? accuracyMeters = null,
        double? altitudeMeters = null,
        double? courseDegrees = null)
    {
        if (latitude is < -90 or > 90 || double.IsNaN(latitude))
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180 || double.IsNaN(longitude))
            throw new ArgumentOutOfRangeException(nameof(longitude));
        if (speedMetersPerSecond is < 0)
            throw new ArgumentOutOfRangeException(nameof(speedMetersPerSecond));
        if (accuracyMeters is < 0)
            throw new ArgumentOutOfRangeException(nameof(accuracyMeters));
        if (altitudeMeters is { } alt && !double.IsFinite(alt))
            throw new ArgumentOutOfRangeException(nameof(altitudeMeters));
        if (courseDegrees is { } crs && (!double.IsFinite(crs) || crs < 0 || crs > 360))
            throw new ArgumentOutOfRangeException(nameof(courseDegrees));

        Latitude = latitude;
        Longitude = longitude;
        Timestamp = timestamp;
        SpeedMetersPerSecond = speedMetersPerSecond;
        AccuracyMeters = accuracyMeters;
        AltitudeMeters = altitudeMeters;
        CourseDegrees = courseDegrees is { } c && c >= 360 ? 0 : courseDegrees;
    }

    public double Latitude { get; }
    public double Longitude { get; }
    public DateTimeOffset Timestamp { get; }
    public double? SpeedMetersPerSecond { get; }
    public double? AccuracyMeters { get; }
    public double? AltitudeMeters { get; }
    public double? CourseDegrees { get; }

    public double DistanceMetersTo(LocationSample other)
    {
        const double EarthRadiusMeters = 6_371_000;
        var lat1 = ToRadians(Latitude);
        var lat2 = ToRadians(other.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
