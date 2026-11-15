using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Trips;

public sealed class TripRecorderFeedTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private sealed class FakeFeed : ILiveLocationFeed
    {
        public bool AcquireResult { get; set; } = true;
        public Exception? AcquireException { get; set; }
        public int AcquireCalls { get; private set; }
        public int ReleaseCalls { get; private set; }
        public LocationSample? LatestSample { get; private set; }
        public event Action<LocationSample>? SampleReceived;

        public Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
        {
            AcquireCalls++;
            if (AcquireException is not null)
                throw AcquireException;
            return Task.FromResult(AcquireResult);
        }

        public Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            ReleaseCalls++;
            return Task.CompletedTask;
        }

        public void Emit(LocationSample sample)
        {
            LatestSample = sample;
            SampleReceived?.Invoke(sample);
        }
    }

    private sealed class FakeRepository : ITripRepository
    {
        public List<Trip> Saved { get; } = [];
        public List<Guid> Completed { get; } = [];
        public List<LocationSample> Appended { get; } = [];

        public Task SaveAsync(Trip trip, CancellationToken cancellationToken = default)
        {
            Saved.Add(trip);
            return Task.CompletedTask;
        }

        public Task CreateAsync(Guid id, DateTimeOffset startedAt, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AppendSamplesAsync(Guid tripId, int firstSeq, IReadOnlyList<LocationSample> samples, CancellationToken cancellationToken = default)
        {
            Appended.AddRange(samples);
            return Task.CompletedTask;
        }

        public Task CompleteAsync(Guid tripId, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
        {
            Completed.Add(tripId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> ListUnfinishedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Trip>>([]);

        public Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Saved.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Trip>>(Saved);

        public Task<IReadOnlyList<TripHeader>> ListHeadersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TripHeader>>([]);
    }

    private sealed class NoOpKeepAlive : IRecordingKeepAlive
    {
        public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => T0;
    }

    private static (TripRecorder Recorder, FakeFeed Feed, FakeRepository Repo) Create()
    {
        var feed = new FakeFeed();
        var repo = new FakeRepository();
        return (new TripRecorder(feed, repo, new FixedTimeProvider(), new NoOpKeepAlive()), feed, repo);
    }

    [Fact]
    public async Task Start_AcquiresFeed()
    {
        var (recorder, feed, _) = Create();

        await recorder.StartAsync();

        Assert.Equal(1, feed.AcquireCalls);
    }

    [Fact]
    public async Task Stop_ReleasesFeed()
    {
        var (recorder, feed, _) = Create();
        await recorder.StartAsync();

        await recorder.StopAsync();

        Assert.Equal(1, feed.ReleaseCalls);
    }

    [Fact]
    public async Task FeedSamples_AreAddedToCurrentTrip()
    {
        var (recorder, feed, _) = Create();
        var trip = await recorder.StartAsync();

        var sample = new LocationSample(41.0, 29.0, T0, 10, 5);
        feed.Emit(sample);

        Assert.Single(trip.Route.Samples);
        Assert.Same(sample, trip.Route.Samples[0]);
    }

    [Fact]
    public async Task FeedSamples_AfterStop_AreNotAddedToSavedTrip()
    {
        var (recorder, feed, repo) = Create();
        await recorder.StartAsync();
        feed.Emit(new LocationSample(41.0, 29.0, T0));

        var stopped = await recorder.StopAsync();
        feed.Emit(new LocationSample(41.1, 29.1, T0.AddSeconds(1)));

        Assert.Single(stopped.Route.Samples);
        Assert.Equal(stopped.Id, Assert.Single(repo.Completed));
        Assert.Single(repo.Appended);
    }

    [Fact]
    public async Task Stop_SavesTripContainingFeedSamples()
    {
        var (recorder, feed, repo) = Create();
        await recorder.StartAsync();
        feed.Emit(new LocationSample(41.0, 29.0, T0));
        feed.Emit(new LocationSample(41.01, 29.0, T0.AddSeconds(30)));

        await recorder.StopAsync();

        Assert.Equal(2, repo.Appended.Count);
        Assert.Single(repo.Completed);
    }

    [Fact]
    public async Task FailedAcquire_ThrowsAndFeedSamplesAreIgnored()
    {
        var (recorder, feed, repo) = Create();
        feed.AcquireResult = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync());
        feed.Emit(new LocationSample(41.0, 29.0, T0));

        Assert.Null(recorder.CurrentTrip);
        Assert.Empty(repo.Completed);
    }

    [Fact]
    public async Task ThrowingAcquire_Propagates_AndFeedSamplesAreIgnored()
    {
        var (recorder, feed, repo) = Create();
        feed.AcquireException = new IOException("x");

        await Assert.ThrowsAsync<IOException>(() => recorder.StartAsync());
        feed.Emit(new LocationSample(41.0, 29.0, T0));

        Assert.Null(recorder.CurrentTrip);
        Assert.Empty(repo.Completed);
    }
}
