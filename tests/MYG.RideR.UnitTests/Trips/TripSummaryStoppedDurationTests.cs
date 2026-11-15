using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Trips;

public sealed class TripSummaryStoppedDurationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    // Speeds below are in m/s. Threshold is 3 km/h (~0.833 m/s).
    private const double StoppedMps = 0;      // 0 km/h
    private const double SlowStoppedMps = 0.5; // 1.8 km/h
    private const double MovingMps = 10;      // 36 km/h

    private static LocationSample Sample(double? speedMps, DateTimeOffset at) =>
        new(41.0, 29.0, at, speedMps);

    [Fact]
    public void AlwaysMoving_HasZeroStoppedDuration()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(MovingMps, T0));
        trip.AddSample(Sample(MovingMps, T0.AddMinutes(1)));
        trip.AddSample(Sample(MovingMps, T0.AddMinutes(2)));
        trip.Complete(T0.AddMinutes(3));

        Assert.Equal(TimeSpan.Zero, TripSummary.From(trip, T0.AddHours(1)).StoppedDuration);
    }

    [Fact]
    public void AlwaysStopped_CompletedTrip_SumsIntervalsBetweenSamples()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(StoppedMps, T0));
        trip.AddSample(Sample(SlowStoppedMps, T0.AddMinutes(1)));
        trip.AddSample(Sample(StoppedMps, T0.AddMinutes(2)));
        trip.Complete(T0.AddMinutes(2));

        Assert.Equal(TimeSpan.FromMinutes(2), TripSummary.From(trip, T0.AddHours(1)).StoppedDuration);
    }

    [Fact]
    public void MixedTrip_SumsOnlyStoppedIntervals()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(StoppedMps, T0));                    // stopped for 1 min
        trip.AddSample(Sample(MovingMps, T0.AddMinutes(1)));       // moving for 2 min
        trip.AddSample(Sample(SlowStoppedMps, T0.AddMinutes(3)));  // stopped for 1 min
        trip.AddSample(Sample(MovingMps, T0.AddMinutes(4)));       // last, moving
        trip.Complete(T0.AddMinutes(5));

        Assert.Equal(TimeSpan.FromMinutes(2), TripSummary.From(trip, T0.AddHours(1)).StoppedDuration);
    }

    [Fact]
    public void CompletedTrip_LastStoppedSample_CountsTailUntilEndedAt()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(MovingMps, T0));
        trip.AddSample(Sample(StoppedMps, T0.AddMinutes(1)));
        trip.Complete(T0.AddMinutes(4));

        Assert.Equal(TimeSpan.FromMinutes(3), TripSummary.From(trip, T0.AddHours(1)).StoppedDuration);
    }

    [Fact]
    public void RecordingTrip_LastStoppedSample_CountsUntilNow()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(MovingMps, T0));
        trip.AddSample(Sample(StoppedMps, T0.AddMinutes(1)));

        var summary = TripSummary.From(trip, T0.AddMinutes(6));

        Assert.Equal(TimeSpan.FromMinutes(5), summary.StoppedDuration);
    }

    [Fact]
    public void RecordingTrip_LastSampleMoving_DoesNotCountTail()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(MovingMps, T0));

        Assert.Equal(TimeSpan.Zero, TripSummary.From(trip, T0.AddMinutes(5)).StoppedDuration);
    }

    [Fact]
    public void NullSpeedSample_IsNotTreatedAsStopped()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(null, T0));                          // unknown speed, not stopped
        trip.AddSample(Sample(StoppedMps, T0.AddMinutes(1)));      // stopped for 1 min
        trip.AddSample(Sample(MovingMps, T0.AddMinutes(2)));
        trip.Complete(T0.AddMinutes(3));

        Assert.Equal(TimeSpan.FromMinutes(1), TripSummary.From(trip, T0.AddHours(1)).StoppedDuration);
    }

    [Fact]
    public void NullSpeedLastSample_WhileRecording_IsNotTreatedAsStopped()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(Sample(null, T0));

        Assert.Equal(TimeSpan.Zero, TripSummary.From(trip, T0.AddMinutes(5)).StoppedDuration);
    }
}
