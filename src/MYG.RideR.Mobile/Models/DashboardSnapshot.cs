namespace MYG.RideR.Mobile.Models;

public sealed record DashboardSnapshot(
    int SpeedKmh,
    string GpsStatus,
    string TripTime,
    double DistanceKm,
    int AverageSpeedKmh,
    int MaximumSpeedKmh);
