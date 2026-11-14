using Microsoft.Data.Sqlite;
using MYG.RideR.Core.Domain;
using MYG.RideR.Infrastructure.Persistence;

namespace MYG.RideR.UnitTests.Persistence;

public sealed class SqliteTripRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"myg-test-{Guid.NewGuid():N}.db");
    private readonly SqliteTripRepository _repo;

    public SqliteTripRepositoryTests()
    {
        _repo = new SqliteTripRepository(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    private static Trip CreateCompletedTrip()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(new LocationSample(41.0, 29.0, T0, 10, 5));
        trip.AddSample(new LocationSample(41.01, 29.0, T0.AddMinutes(1), null, null));
        trip.Complete(T0.AddMinutes(2));
        return trip;
    }

    [Fact]
    public async Task Get_Unknown_ReturnsNull()
    {
        Assert.Null(await _repo.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SaveThenGet_RoundTripsTrip()
    {
        var trip = CreateCompletedTrip();

        await _repo.SaveAsync(trip);
        var loaded = await _repo.GetAsync(trip.Id);

        Assert.NotNull(loaded);
        Assert.Equal(trip.StartedAt, loaded.StartedAt);
        Assert.Equal(trip.EndedAt, loaded.EndedAt);
        Assert.Equal(TripStatus.Completed, loaded.Status);
        Assert.Equal(trip.Route.Samples, loaded.Route.Samples);
        Assert.Equal(trip.DistanceKm, loaded.DistanceKm, 6);
        Assert.Equal(trip.MaximumSpeedKmh, loaded.MaximumSpeedKmh, 6);
    }

    [Fact]
    public async Task Save_Twice_ReplacesSamples()
    {
        var trip = CreateCompletedTrip();
        await _repo.SaveAsync(trip);
        await _repo.SaveAsync(trip);

        var loaded = await _repo.GetAsync(trip.Id);

        Assert.Equal(2, loaded!.Route.Samples.Count);
        Assert.Single(await _repo.ListAsync());
    }

    [Fact]
    public async Task List_ReturnsNewestFirst()
    {
        var older = Trip.Start(T0);
        older.Complete(T0.AddMinutes(5));
        var newer = Trip.Start(T0.AddDays(1));
        newer.Complete(T0.AddDays(1).AddMinutes(5));
        await _repo.SaveAsync(older);
        await _repo.SaveAsync(newer);

        var list = await _repo.ListAsync();

        Assert.Equal([newer.Id, older.Id], list.Select(t => t.Id));
    }

    [Fact]
    public async Task Save_RecordingTrip_RestoresAsRecording()
    {
        var trip = Trip.Start(T0);
        trip.AddSample(new LocationSample(1, 1, T0));
        await _repo.SaveAsync(trip);

        var loaded = await _repo.GetAsync(trip.Id);

        Assert.Equal(TripStatus.Recording, loaded!.Status);
        Assert.Null(loaded.EndedAt);
    }
}
