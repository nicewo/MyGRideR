using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Location;
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
        public List<Guid> Created { get; } = [];
        public List<(int FirstSeq, int Count)> Appends { get; } = [];
        public List<(Guid Id, DateTimeOffset EndedAt)> Completed { get; } = [];
        public List<Trip> Unfinished { get; } = [];
        public bool FailNextSave { get; set; }
        public bool FailNextAppend { get; set; }
        public bool FailNextCreate { get; set; }

        public Task SaveAsync(Trip trip, CancellationToken cancellationToken = default)
        {
            Saved.Add(trip);
            return Task.CompletedTask;
        }

        public Task CreateAsync(Guid id, DateTimeOffset startedAt, CancellationToken cancellationToken = default)
        {
            if (FailNextCreate)
            {
                FailNextCreate = false;
                throw new IOException("create failed");
            }

            Created.Add(id);
            return Task.CompletedTask;
        }

        public Task AppendSamplesAsync(Guid tripId, int firstSeq, IReadOnlyList<LocationSample> samples, CancellationToken cancellationToken = default)
        {
            if (FailNextAppend)
            {
                FailNextAppend = false;
                throw new IOException("append failed");
            }

            Appends.Add((firstSeq, samples.Count));
            return Task.CompletedTask;
        }

        public Task CompleteAsync(Guid tripId, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new IOException("save failed");
            }

            Completed.Add((tripId, endedAt));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Trip>> ListUnfinishedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Trip>>(Unfinished);

        public Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Saved.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Trip>>(Saved);

        public Task<IReadOnlyList<TripHeader>> ListHeadersAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TripHeader>>([]);
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FakeKeepAlive : IRecordingKeepAlive
    {
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public bool FailStart { get; set; }

        public Task StartAsync(CancellationToken ct = default)
        {
            StartCalls++;
            if (FailStart)
                throw new InvalidOperationException("keep-alive failed");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken ct = default)
        {
            StopCalls++;
            return Task.CompletedTask;
        }
    }

    private static (TripRecorder Recorder, FakeLocationService Location, FakeRepository Repo, FakeTimeProvider Time) Create()
    {
        var (recorder, location, repo, time, _) = CreateWithKeepAlive();
        return (recorder, location, repo, time);
    }

    private static (TripRecorder Recorder, FakeLocationService Location, FakeRepository Repo, FakeTimeProvider Time, FakeKeepAlive KeepAlive) CreateWithKeepAlive()
    {
        var location = new FakeLocationService();
        var repo = new FakeRepository();
        var time = new FakeTimeProvider();
        var keepAlive = new FakeKeepAlive();
        return (new TripRecorder(new LiveLocationFeed(location), repo, time, keepAlive), location, repo, time, keepAlive);
    }

    [Fact]
    public async Task KeepAlive_StartedOnceAfterStart()
    {
        var (recorder, _, _, _, keepAlive) = CreateWithKeepAlive();

        await recorder.StartAsync();

        Assert.Equal(1, keepAlive.StartCalls);
        Assert.Equal(0, keepAlive.StopCalls);
    }

    [Fact]
    public async Task KeepAlive_StoppedOnceAfterStop()
    {
        var (recorder, _, _, _, keepAlive) = CreateWithKeepAlive();
        await recorder.StartAsync();

        await recorder.StopAsync();

        Assert.Equal(1, keepAlive.StopCalls);
    }

    [Fact]
    public async Task KeepAlive_StartFailure_StillRecords()
    {
        var (recorder, _, _, _, keepAlive) = CreateWithKeepAlive();
        keepAlive.FailStart = true;

        var trip = await recorder.StartAsync();

        Assert.Same(trip, recorder.CurrentTrip);
        await recorder.StopAsync();
        Assert.Equal(0, keepAlive.StopCalls);
    }

    [Fact]
    public async Task KeepAlive_NotStarted_WhenAcquireFails()
    {
        var (recorder, location, _, _, keepAlive) = CreateWithKeepAlive();
        location.StartResult = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync());

        Assert.Equal(0, keepAlive.StartCalls);
    }

    [Fact]
    public async Task KeepAlive_NotStarted_WhenCreateFails()
    {
        var (recorder, _, repo, _, keepAlive) = CreateWithKeepAlive();
        repo.FailNextCreate = true;

        await Assert.ThrowsAsync<IOException>(() => recorder.StartAsync());

        Assert.Equal(0, keepAlive.StartCalls);
    }

    [Fact]
    public async Task KeepAlive_StoppedEvenWhenSaveFails_AndNotStoppedAgainOnRetry()
    {
        var (recorder, _, repo, _, keepAlive) = CreateWithKeepAlive();
        await recorder.StartAsync();
        repo.FailNextSave = true;

        await Assert.ThrowsAsync<IOException>(() => recorder.StopAsync());
        Assert.Equal(1, keepAlive.StopCalls);

        await recorder.StopAsync();
        Assert.Equal(1, keepAlive.StopCalls);
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
    public void GetCurrentSummary_WithoutTrip_ReturnsNull()
    {
        var (recorder, _, _, time) = Create();

        Assert.Null(recorder.GetCurrentSummary(time.Now));
    }

    [Fact]
    public async Task GetCurrentSummary_MatchesTripSummaryFrom()
    {
        var (recorder, location, _, time) = Create();
        var trip = await recorder.StartAsync();
        location.Raise(new LocationSample(0, 0, time.Now));
        location.Raise(new LocationSample(0.01, 0, time.Now.AddMinutes(1)));
        var now = time.Now.AddMinutes(2);

        var summary = recorder.GetCurrentSummary(now);

        Assert.Equal(TripSummary.From(trip, now), summary);
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
        Assert.Single(repo.Completed);
        Assert.Null(recorder.CurrentTrip);
    }

    [Fact]
    public async Task Stop_WithoutTrip_Throws()
    {
        var (recorder, _, _, _) = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StopAsync());
    }

    [Fact]
    public async Task Stop_WhenSaveFails_KeepsTripAndThrows()
    {
        var (recorder, _, repo, _) = Create();
        var trip = await recorder.StartAsync();
        repo.FailNextSave = true;

        await Assert.ThrowsAsync<IOException>(() => recorder.StopAsync());

        Assert.Same(trip, recorder.CurrentTrip);
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Empty(repo.Completed);
    }

    [Fact]
    public async Task Stop_RetryAfterSaveFailure_SavesAndClearsTrip()
    {
        var (recorder, location, repo, _) = Create();
        var trip = await recorder.StartAsync();
        repo.FailNextSave = true;
        await Assert.ThrowsAsync<IOException>(() => recorder.StopAsync());

        var stopped = await recorder.StopAsync();

        Assert.Same(trip, stopped);
        Assert.Single(repo.Completed);
        Assert.Null(recorder.CurrentTrip);
        Assert.False(location.Started);
    }

    [Fact]
    public async Task Start_WhenUnsavedTripExists_Throws()
    {
        var (recorder, _, repo, _) = Create();
        await recorder.StartAsync();
        repo.FailNextSave = true;
        await Assert.ThrowsAsync<IOException>(() => recorder.StopAsync());

        await Assert.ThrowsAsync<InvalidOperationException>(() => recorder.StartAsync());
    }

    private static void Emit(FakeLocationService location, DateTimeOffset t0, int count, int startIndex = 0)
    {
        for (var i = 0; i < count; i++)
            location.Raise(new LocationSample(0.0001 * (startIndex + i), 0, t0.AddSeconds(startIndex + i)));
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);
        Assert.True(condition());
    }

    [Fact]
    public async Task Start_CreatesTripRow()
    {
        var (recorder, _, repo, _) = Create();

        var trip = await recorder.StartAsync();

        Assert.Equal(trip.Id, Assert.Single(repo.Created));
    }

    [Fact]
    public async Task Start_WhenCreateFails_ReleasesFeedAndClearsTrip()
    {
        var (recorder, location, repo, _) = Create();
        repo.FailNextCreate = true;

        await Assert.ThrowsAsync<IOException>(() => recorder.StartAsync());

        Assert.Null(recorder.CurrentTrip);
        Assert.False(location.Started);
    }

    [Fact]
    public async Task Flush_After15Samples_AppendsBatch()
    {
        var (recorder, location, repo, time) = Create();
        await recorder.StartAsync();

        Emit(location, time.Now, 15);

        await WaitUntilAsync(() => repo.Appends.Count == 1);
        Assert.Equal((0, 15), repo.Appends[0]);
    }

    [Fact]
    public async Task Flush_After15Seconds_AppendsPendingSamples()
    {
        var (recorder, location, repo, time) = Create();
        await recorder.StartAsync();
        var t0 = time.Now;
        location.Raise(new LocationSample(0, 0, t0));
        time.Now = t0.AddSeconds(16);

        location.Raise(new LocationSample(0.0001, 0, t0.AddSeconds(16)));

        await WaitUntilAsync(() => repo.Appends.Count == 1);
        Assert.Equal((0, 2), repo.Appends[0]);
    }

    [Fact]
    public async Task Flush_AfterAppendFailure_RetriesWithoutLosingSamples()
    {
        var (recorder, location, repo, time) = Create();
        await recorder.StartAsync();
        repo.FailNextAppend = true;

        Emit(location, time.Now, 15);
        await Task.Delay(100);
        Assert.Empty(repo.Appends);

        Emit(location, time.Now, 1, startIndex: 15);

        await WaitUntilAsync(() => repo.Appends.Count == 1);
        Assert.Equal((0, 16), repo.Appends[0]);
    }

    [Fact]
    public async Task Stop_WritesRemainingSamplesThenCompletes()
    {
        var (recorder, location, repo, time) = Create();
        var trip = await recorder.StartAsync();
        Emit(location, time.Now, 5);
        time.Now = time.Now.AddMinutes(1);

        await recorder.StopAsync();

        Assert.Equal((0, 5), Assert.Single(repo.Appends));
        Assert.Equal((trip.Id, time.Now), Assert.Single(repo.Completed));
    }

    [Fact]
    public async Task Recover_ClosesUnfinishedTripsAtLastSampleTime()
    {
        var (recorder, _, repo, time) = Create();
        var t0 = time.Now;
        var withSamples = Trip.Restore(Guid.NewGuid(), t0, null,
            [new LocationSample(0, 0, t0.AddSeconds(1)), new LocationSample(0.001, 0, t0.AddSeconds(30))]);
        var empty = Trip.Restore(Guid.NewGuid(), t0.AddHours(1), null, []);
        repo.Unfinished.AddRange([withSamples, empty]);

        var closed = await recorder.RecoverUnfinishedAsync();

        Assert.Equal(2, closed);
        Assert.Contains((withSamples.Id, t0.AddSeconds(30)), repo.Completed);
        Assert.Contains((empty.Id, t0.AddHours(1)), repo.Completed);
    }
}
