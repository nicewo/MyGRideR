using Microsoft.Data.Sqlite;
using MYG.RideR.Application.Trips;
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
    public async Task ListHeaders_ReturnsSummariesNewestFirst()
    {
        var older = CreateCompletedTrip();
        var newer = Trip.Start(T0.AddDays(1));
        newer.Complete(T0.AddDays(1).AddMinutes(5));
        await _repo.SaveAsync(older);
        await _repo.SaveAsync(newer);

        var headers = await _repo.ListHeadersAsync();

        Assert.Equal([newer.Id, older.Id], headers.Select(h => h.Id));
        var expected = TripSummary.From(older, older.EndedAt!.Value);
        var h = headers[1];
        Assert.Equal(expected.DistanceKm, h.DistanceKm, 6);
        Assert.Equal(expected.MaximumSpeedKmh, h.MaximumSpeedKmh, 6);
        Assert.Equal(expected.StoppedDuration, h.StoppedDuration);
        Assert.Equal(older.EndedAt, h.EndedAt);
    }

    [Fact]
    public async Task ListHeaders_BackfillsNullSummaryColumns()
    {
        var trip = CreateCompletedTrip();
        await _repo.SaveAsync(trip);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString()))
        {
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Trips SET DistanceM = NULL, MaxSpeedKmh = NULL, StoppedSeconds = NULL";
            await cmd.ExecuteNonQueryAsync();
        }

        var headers = await _repo.ListHeadersAsync();

        Assert.Equal(trip.DistanceKm, headers[0].DistanceKm, 6);
        Assert.Equal(trip.MaximumSpeedKmh, headers[0].MaximumSpeedKmh, 6);

        await using var check = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
        await check.OpenAsync();
        await using var q = check.CreateCommand();
        q.CommandText = "SELECT COUNT(*) FROM Trips WHERE DistanceM IS NULL";
        Assert.Equal(0L, await q.ExecuteScalarAsync());
    }

    [Fact]
    public async Task CompleteAsync_FillsSummaryColumns()
    {
        var trip = CreateCompletedTrip();
        await _repo.CreateAsync(trip.Id, trip.StartedAt);
        await _repo.AppendSamplesAsync(trip.Id, 0, trip.Route.Samples);
        await _repo.CompleteAsync(trip.Id, trip.EndedAt!.Value);

        var h = Assert.Single(await _repo.ListHeadersAsync());

        Assert.Equal(trip.DistanceKm, h.DistanceKm, 6);
        Assert.Equal(trip.MaximumSpeedKmh, h.MaximumSpeedKmh, 6);
    }

    [Fact]
    public void Aggregate_Headers_MatchesAggregateTrips()
    {
        var a = CreateCompletedTrip();
        var b = Trip.Start(T0.AddDays(1));
        b.AddSample(new LocationSample(41.0, 29.0, T0.AddDays(1), 20, 5));
        b.AddSample(new LocationSample(41.02, 29.0, T0.AddDays(1).AddMinutes(3), 20, 5));
        b.Complete(T0.AddDays(1).AddMinutes(10));
        var now = T0.AddDays(2);

        var headers = new[] { a, b }.Select(t =>
        {
            var s = TripSummary.From(t, t.EndedAt!.Value);
            return new TripHeader(t.Id, t.StartedAt, t.EndedAt, s.DistanceKm, s.MaximumSpeedKmh, s.StoppedDuration);
        });

        Assert.Equal(TripStatistics.Aggregate(new[] { a, b }, now), TripStatistics.Aggregate(headers, now));
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
