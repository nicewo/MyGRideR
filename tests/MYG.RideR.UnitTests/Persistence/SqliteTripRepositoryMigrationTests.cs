using Microsoft.Data.Sqlite;
using MYG.RideR.Core.Domain;
using MYG.RideR.Infrastructure.Persistence;

namespace MYG.RideR.UnitTests.Persistence;

public sealed class SqliteTripRepositoryMigrationTests : IDisposable
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"myg-legacy-{Guid.NewGuid():N}.db");

    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _path }.ToString();

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_path))
            File.Delete(_path);
    }

    // Creates a Samples table in the pre-AltitudeM/CourseDeg schema, as shipped before the migration.
    private Guid CreateLegacyDatabase()
    {
        var tripId = Guid.NewGuid();

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE Trips (
                    Id TEXT PRIMARY KEY,
                    StartedAt TEXT NOT NULL,
                    EndedAt TEXT NULL
                );
                CREATE TABLE Samples (
                    TripId TEXT NOT NULL REFERENCES Trips(Id) ON DELETE CASCADE,
                    Seq INTEGER NOT NULL,
                    Latitude REAL NOT NULL,
                    Longitude REAL NOT NULL,
                    Timestamp TEXT NOT NULL,
                    SpeedMps REAL NULL,
                    AccuracyM REAL NULL,
                    PRIMARY KEY (TripId, Seq)
                );
                """;
            cmd.ExecuteNonQuery();
        }

        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = "INSERT INTO Trips (Id, StartedAt, EndedAt) VALUES ($id, $start, $end)";
            insert.Parameters.AddWithValue("$id", tripId.ToString());
            insert.Parameters.AddWithValue("$start", T0.ToString("O"));
            insert.Parameters.AddWithValue("$end", T0.AddMinutes(2).ToString("O"));
            insert.ExecuteNonQuery();
        }

        using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO Samples (TripId, Seq, Latitude, Longitude, Timestamp, SpeedMps, AccuracyM)
                VALUES ($id, $seq, $lat, $lon, $ts, $speed, $acc)
                """;
            insert.Parameters.AddWithValue("$id", tripId.ToString());
            insert.Parameters.AddWithValue("$seq", 0);
            insert.Parameters.AddWithValue("$lat", 41.0);
            insert.Parameters.AddWithValue("$lon", 29.0);
            insert.Parameters.AddWithValue("$ts", T0.ToString("O"));
            insert.Parameters.AddWithValue("$speed", 10.0);
            insert.Parameters.AddWithValue("$acc", 5.0);
            insert.ExecuteNonQuery();

            insert.Parameters.Clear();
            insert.Parameters.AddWithValue("$id", tripId.ToString());
            insert.Parameters.AddWithValue("$seq", 1);
            insert.Parameters.AddWithValue("$lat", 41.01);
            insert.Parameters.AddWithValue("$lon", 29.0);
            insert.Parameters.AddWithValue("$ts", T0.AddMinutes(1).ToString("O"));
            insert.Parameters.AddWithValue("$speed", DBNull.Value);
            insert.Parameters.AddWithValue("$acc", DBNull.Value);
            insert.ExecuteNonQuery();
        }

        return tripId;
    }

    private static IReadOnlyList<string> SamplesColumns(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA table_info(Samples)";
        using var reader = cmd.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1));
        return columns;
    }

    [Fact]
    public async Task LegacySchema_IsMigrated_AndOldRowsReadWithNullAltitudeAndCourse()
    {
        var tripId = CreateLegacyDatabase();
        var repo = new SqliteTripRepository(ConnectionString);

        var loaded = await repo.GetAsync(tripId);

        Assert.NotNull(loaded);
        Assert.Equal(TripStatus.Completed, loaded.Status);
        Assert.Equal(2, loaded.Route.Samples.Count);

        var first = loaded.Route.Samples[0];
        Assert.Equal(41.0, first.Latitude);
        Assert.Equal(10.0, first.SpeedMetersPerSecond);
        Assert.Null(first.AltitudeMeters);
        Assert.Null(first.CourseDegrees);

        var second = loaded.Route.Samples[1];
        Assert.Null(second.SpeedMetersPerSecond);
        Assert.Null(second.AltitudeMeters);
        Assert.Null(second.CourseDegrees);

        Assert.Contains("AltitudeM", SamplesColumns(ConnectionString));
        Assert.Contains("CourseDeg", SamplesColumns(ConnectionString));
    }

    [Fact]
    public async Task LegacySchema_ListAlsoWorksAfterMigration()
    {
        CreateLegacyDatabase();
        var repo = new SqliteTripRepository(ConnectionString);

        var list = await repo.ListAsync();

        Assert.Single(list);
        Assert.Equal(2, list[0].Route.Samples.Count);
    }

    [Fact]
    public async Task LegacySchema_NewSampleWithAltitudeAndCourse_RoundTripsAfterMigration()
    {
        CreateLegacyDatabase();
        var repo = new SqliteTripRepository(ConnectionString);

        var trip = Trip.Start(T0.AddDays(1));
        trip.AddSample(new LocationSample(41.0, 29.0, T0.AddDays(1), 8, 4, altitudeMeters: 350.25, courseDegrees: 91.5));
        trip.Complete(T0.AddDays(1).AddMinutes(1));
        await repo.SaveAsync(trip);

        var loaded = await repo.GetAsync(trip.Id);

        var sample = Assert.Single(loaded!.Route.Samples);
        Assert.Equal(350.25, sample.AltitudeMeters);
        Assert.Equal(91.5, sample.CourseDegrees);
    }

    [Fact]
    public async Task AlreadyMigratedDatabase_CanBeOpenedAgain()
    {
        var tripId = CreateLegacyDatabase();
        await new SqliteTripRepository(ConnectionString).GetAsync(tripId);

        var reopened = await new SqliteTripRepository(ConnectionString).GetAsync(tripId);

        Assert.NotNull(reopened);
        Assert.Equal(2, reopened.Route.Samples.Count);
        Assert.Single(SamplesColumns(ConnectionString), c => c == "AltitudeM");
    }
}
