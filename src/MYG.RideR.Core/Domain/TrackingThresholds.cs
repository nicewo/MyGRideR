namespace MYG.RideR.Core.Domain;

public static class TrackingThresholds
{
    public const double MaxAccuracyMeters = 25;
    public const double MinSegmentMeters = 5;
    public const double MaxPlausibleSpeedMps = 83.3;
    public const int JumpReanchorCount = 3;
}
