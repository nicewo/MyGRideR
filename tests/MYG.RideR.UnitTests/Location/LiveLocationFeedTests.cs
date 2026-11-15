using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Location;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.UnitTests.Location;

public sealed class LiveLocationFeedTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private sealed class FakeLocationService : ILocationService
    {
        public bool StartResult { get; set; } = true;
        public Exception? StartException { get; set; }
        public int StartCalls { get; private set; }
        public int StopCalls { get; private set; }
        public event Action<LocationSample>? LocationChanged;

        public Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            StartCalls++;
            if (StartException is not null)
                throw StartException;
            return Task.FromResult(StartResult);
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCalls++;
            return Task.CompletedTask;
        }

        public void Raise(LocationSample sample) => LocationChanged?.Invoke(sample);
    }

    [Fact]
    public async Task FirstAcquire_StartsService_SecondAcquire_DoesNotStartAgain()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);

        Assert.True(await feed.AcquireAsync());
        Assert.True(await feed.AcquireAsync());

        Assert.Equal(1, service.StartCalls);
    }

    [Fact]
    public async Task LastRelease_StopsService_IntermediateRelease_DoesNot()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);
        await feed.AcquireAsync();
        await feed.AcquireAsync();

        await feed.ReleaseAsync();
        Assert.Equal(0, service.StopCalls);

        await feed.ReleaseAsync();
        Assert.Equal(1, service.StopCalls);
    }

    [Fact]
    public async Task Release_WithoutAcquire_DoesNotStopService()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);

        await feed.ReleaseAsync();

        Assert.Equal(0, service.StopCalls);
    }

    [Fact]
    public async Task LatestSample_IsUpdatedFromService()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);
        await feed.AcquireAsync();

        var first = new LocationSample(41.0, 29.0, T0);
        var second = new LocationSample(41.1, 29.1, T0.AddSeconds(1));
        service.Raise(first);
        Assert.Same(first, feed.LatestSample);

        service.Raise(second);
        Assert.Same(second, feed.LatestSample);
    }

    [Fact]
    public async Task SampleReceived_IsRaisedForEachSample()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);
        var received = new List<LocationSample>();
        feed.SampleReceived += received.Add;
        await feed.AcquireAsync();

        var sample = new LocationSample(41.0, 29.0, T0);
        service.Raise(sample);

        Assert.Single(received);
        Assert.Same(sample, received[0]);
    }

    [Fact]
    public async Task AfterFinalRelease_SamplesAreIgnored()
    {
        var service = new FakeLocationService();
        var feed = new LiveLocationFeed(service);
        var received = new List<LocationSample>();
        feed.SampleReceived += received.Add;
        await feed.AcquireAsync();
        service.Raise(new LocationSample(41.0, 29.0, T0));

        await feed.ReleaseAsync();
        service.Raise(new LocationSample(41.1, 29.1, T0.AddSeconds(1)));

        Assert.Single(received);
        Assert.Null(feed.LatestSample);
    }

    [Fact]
    public async Task FailedStart_ReturnsFalse_AndDoesNotCountAsHolder()
    {
        var service = new FakeLocationService { StartResult = false };
        var feed = new LiveLocationFeed(service);

        Assert.False(await feed.AcquireAsync());
        await feed.ReleaseAsync();

        Assert.Equal(0, service.StopCalls);
    }

    [Fact]
    public async Task FailedStart_NextAcquire_RetriesStart()
    {
        var service = new FakeLocationService { StartResult = false };
        var feed = new LiveLocationFeed(service);
        await feed.AcquireAsync();

        service.StartResult = true;
        Assert.True(await feed.AcquireAsync());

        Assert.Equal(2, service.StartCalls);

        await feed.ReleaseAsync();
        Assert.Equal(1, service.StopCalls);
    }

    [Fact]
    public async Task FailedStart_SamplesFromServiceAreNotTracked()
    {
        var service = new FakeLocationService { StartResult = false };
        var feed = new LiveLocationFeed(service);
        await feed.AcquireAsync();

        service.Raise(new LocationSample(41.0, 29.0, T0));

        Assert.Null(feed.LatestSample);
    }

    [Fact]
    public async Task ThrowingStart_Propagates_AndUnsubscribes()
    {
        var service = new FakeLocationService { StartException = new IOException("x") };
        var feed = new LiveLocationFeed(service);

        await Assert.ThrowsAsync<IOException>(() => feed.AcquireAsync());
        service.Raise(new LocationSample(41.0, 29.0, T0));

        Assert.Null(feed.LatestSample);
        await feed.ReleaseAsync();
        Assert.Equal(0, service.StopCalls);
    }

    [Fact]
    public async Task ThrowingStart_NextAcquire_RetriesStart()
    {
        var service = new FakeLocationService { StartException = new IOException("x") };
        var feed = new LiveLocationFeed(service);
        await Assert.ThrowsAsync<IOException>(() => feed.AcquireAsync());

        service.StartException = null;
        Assert.True(await feed.AcquireAsync());

        Assert.Equal(2, service.StartCalls);
        await feed.ReleaseAsync();
        Assert.Equal(1, service.StopCalls);
    }
}
