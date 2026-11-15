using Microsoft.Data.Sqlite;
using MYG.RideR.Core.Domain;
using MYG.RideR.Infrastructure.Persistence;

namespace MYG.RideR.UnitTests.Persistence;

public sealed class SqliteTripRepositoryIncrementalTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"myg-test-{Guid.NewGuid():N}.db");
    private readonly SqliteTripRepository _repo;

    public SqliteTripRepositoryIncrementalTests()
    {
        _repo = new SqliteTripRepository(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var f in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            if (File.Exists(f))
                File.Delete(f);
        }
    }

    [Fact]
    public async Task CreateAppendComplete_ThenGet_ReturnsSamplesInOrder()
    {
        var id = Guid.NewGuid();
        await _repo.CreateAsync(id, T0);
        await _repo.AppendSamplesAsync(id, 0,
            [new LocationSample(41.0, 29.0, T0), new LocationSample(41.001, 29.0, T0.AddSeconds(10))]);
        await _repo.AppendSamplesAsync(id, 2, [new LocationSample(41.002, 29.0, T0.AddSeconds(20), 5, 3, 100, 90)]);
        await _repo.CompleteAsync(id, T0.AddSeconds(30));

        var trip = await _repo.GetAsync(id);

        Assert.NotNull(trip);
        Assert.Equal(T0.AddSeconds(30), trip.EndedAt);
        Assert.Equal([41.0, 41.001, 41.002], trip.Route.Samples.Select(s => s.Latitude));
        Assert.Equal(100, trip.Route.Samples[2].AltitudeMeters);
    }

    [Fact]
    public async Task ListUnfinished_ReturnsOnlyOpenTrips()
    {
        var open = Guid.NewGuid();
        var closed = Guid.NewGuid();
        await _repo.CreateAsync(open, T0);
        await _repo.AppendSamplesAsync(open, 0, [new LocationSample(41.0, 29.0, T0)]);
        await _repo.CreateAsync(closed, T0.AddHours(1));
        await _repo.CompleteAsync(closed, T0.AddHours(2));

        var list = await _repo.ListUnfinishedAsync();

        var trip = Assert.Single(list);
        Assert.Equal(open, trip.Id);
        Assert.Single(trip.Route.Samples);
    }
}
