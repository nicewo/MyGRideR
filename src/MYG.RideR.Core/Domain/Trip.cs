namespace MYG.RideR.Core.Domain;

public sealed class Trip
{
    private const double MetersPerSecondToKmh = 3.6;

    private Trip(Guid id, DateTimeOffset startedAt)
    {
        Id = id;
        StartedAt = startedAt;
    }

    public Guid Id { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? EndedAt { get; private set; }
    public TripStatus Status { get; private set; } = TripStatus.Recording;
    public Route Route { get; } = new();
    public double MaximumSpeedKmh { get; private set; }

    public double DistanceKm => Route.DistanceMeters / 1000;

    public TimeSpan Duration(DateTimeOffset now) => (EndedAt ?? now) - StartedAt;

    public double AverageSpeedKmh(DateTimeOffset now)
    {
        var hours = Duration(now).TotalHours;
        return hours <= 0 ? 0 : DistanceKm / hours;
    }

    public static Trip Start(DateTimeOffset startedAt) => new(Guid.NewGuid(), startedAt);

    public static Trip Restore(
        Guid id,
        DateTimeOffset startedAt,
        DateTimeOffset? endedAt,
        IEnumerable<LocationSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var trip = new Trip(id, startedAt);
        foreach (var sample in samples)
            trip.AddSample(sample);
        if (endedAt is { } end)
            trip.Complete(end);

        return trip;
    }

    public void AddSample(LocationSample sample)
    {
        if (Status != TripStatus.Recording)
            throw new InvalidOperationException("Cannot add samples to a completed trip.");

        Route.Add(sample);

        if (sample.SpeedMetersPerSecond is { } speed)
            MaximumSpeedKmh = Math.Max(MaximumSpeedKmh, speed * MetersPerSecondToKmh);
    }

    public void Complete(DateTimeOffset endedAt)
    {
        if (Status == TripStatus.Completed)
            throw new InvalidOperationException("Trip is already completed.");
        if (endedAt < StartedAt)
            throw new ArgumentOutOfRangeException(nameof(endedAt));

        EndedAt = endedAt;
        Status = TripStatus.Completed;
    }
}
