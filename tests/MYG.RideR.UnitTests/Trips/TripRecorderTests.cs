using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Trips;

public sealed class TripRecorderTests
{
    private sealed class FakeLocationService : ILocationService
    {
        public bool StartResult { get; set; } = true;
        public bool Started { get; private set; }
        public event Action<LocationSample>? LocationChanged;

        public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            Started = StartResult;
            return Task.FromResult(StartResult);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Started = false;
            return Task.CompletedTask;
        }

        public void Raise(LocationSample sample) => LocationChanged?.Invoke(sample);
    }

    private sealed class FakeRepository : ITripRepository
    {
        public List<Trip> Saved { get; } = [];

        public Task SaveAsync(Trip trip, CancellationToken cancellationToken = default)
        {
            Saved.Add(trip);
            return Task.CompletedTask;
        }

        public Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Saved.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Trip>>(Saved);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (TripRecorder Recorder, FakeLocationService Location, FakeRepository Repo, FakeTimeProvider Time) Create()
    {
        var location = new FakeLocationService();
        var repo = new FakeRepository();
        var time = new FakeTimeProvider();
        return (new TripRecorder(location, repo, time), location, repo, time);
    }

    [Fact]
    public async Task Start_BeginsRecordingAndStartsLocation()
    {
        var (recorder, location, _, _) = Create();

        var trip = await recorder.StartAsync();

        Assert.Same(trip, recorder.CurrentTrip);
        Assert.True(location.Started);
    }

    [Fact]
    public async Task Start_WhenAlreadyRecording_Throws()
    {
        var (recorder, _, _, _) = Create();
        await recorder.StartAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync());
    }

    [Fact]
    public async Task Start_WhenLocationFails_ThrowsAndClearsTrip()
    {
        var (recorder, location, _, _) = Create();
        location.StartResult = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync());
        Assert.Null(recorder.CurrentTrip);
    }

    [Fact]
    public async Task LocationUpdates_AreAddedToTrip()
    {
        var (recorder, location, _, time) = Create();
        var trip = await recorder.StartAsync();

        location.Raise(new LocationSample(0, 0, time.Now));
        location.Raise(new LocationSample(0.01, 0, time.Now.AddMinutes(1)));

        Assert.Equal(2, trip.Route.Samples.Count);
        Assert.True(trip.DistanceKm > 1);
    }

    [Fact]
    public async Task OutOfOrderSample_IsIgnored()
    {
        var (recorder, location, _, time) = Create();
        var trip = await recorder.StartAsync();

        location.Raise(new LocationSample(0, 0, time.Now));
        location.Raise(new LocationSample(0, 0, time.Now.AddSeconds(-5)));

        Assert.Single(trip.Route.Samples);
    }

    [Fact]
    public async Task Stop_CompletesSavesAndStopsReceivingSamples()
    {
        var (recorder, location, repo, time) = Create();
        var trip = await recorder.StartAsync();
        time.Now = time.Now.AddMinutes(20);

        var stopped = await recorder.StopAsync();
        location.Raise(new LocationSample(0, 0, time.Now));

        Assert.Same(trip, stopped);
        Assert.Equal(TripStatus.Completed, stopped.Status);
        Assert.Equal(TimeSpan.FromMinutes(20), stopped.Duration(time.Now));
        Assert.Single(repo.Saved);
        Assert.Null(recorder.CurrentTrip);
        Assert.Empty(stopped.Route.Samples);
    }

    [Fact]
    public async Task Stop_WithoutTrip_Throws()
    {
        var (recorder, _, _, _) = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StopAsync());
    }
}
