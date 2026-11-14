using Microsoft.Data.Sqlite;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Infrastructure.Persistence;

public sealed class SqliteTripRepository : ITripRepository
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Trips (
            Id TEXT PRIMARY KEY,
            StartedAt TEXT NOT NULL,
            EndedAt TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS Samples (
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

    private readonly string _connectionString;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _initialized;

    public SqliteTripRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task SaveAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trip);

        await using var connection = await OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = "DELETE FROM Trips WHERE Id = $id";
            delete.Parameters.AddWithValue("$id", trip.Id.ToString());
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insert = connection.CreateCommand())
        {
            insert.Transaction = tx;
            insert.CommandText = "INSERT INTO Trips (Id, StartedAt, EndedAt) VALUES ($id, $start, $end)";
            insert.Parameters.AddWithValue("$id", trip.Id.ToString());
            insert.Parameters.AddWithValue("$start", Format(trip.StartedAt));
            insert.Parameters.AddWithValue("$end", trip.EndedAt is { } e ? Format(e) : DBNull.Value);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        var seq = 0;
        foreach (var s in trip.Route.Samples)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Samples (TripId, Seq, Latitude, Longitude, Timestamp, SpeedMps, AccuracyM)
                VALUES ($id, $seq, $lat, $lon, $ts, $speed, $acc)
                """;
            cmd.Parameters.AddWithValue("$id", trip.Id.ToString());
            cmd.Parameters.AddWithValue("$seq", seq++);
            cmd.Parameters.AddWithValue("$lat", s.Latitude);
            cmd.Parameters.AddWithValue("$lon", s.Longitude);
            cmd.Parameters.AddWithValue("$ts", Format(s.Timestamp));
            cmd.Parameters.AddWithValue("$speed", s.SpeedMetersPerSecond is { } sp ? sp : DBNull.Value);
            cmd.Parameters.AddWithValue("$acc", s.AccuracyMeters is { } a ? a : DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, StartedAt, EndedAt FROM Trips WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return await ReadTripAsync(connection, reader, cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var headers = new List<(Guid Id, DateTimeOffset Start, DateTimeOffset? End)>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, StartedAt, EndedAt FROM Trips ORDER BY StartedAt DESC";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                headers.Add(ReadHeader(reader));
        }

        var trips = new List<Trip>(headers.Count);
        foreach (var (id, start, end) in headers)
            trips.Add(Trip.Restore(id, start, end, await ReadSamplesAsync(connection, id, cancellationToken)));

        return trips;
    }

    private async Task<Trip> ReadTripAsync(SqliteConnection connection, SqliteDataReader reader, CancellationToken ct)
    {
        var (id, start, end) = ReadHeader(reader);
        await reader.CloseAsync();
        return Trip.Restore(id, start, end, await ReadSamplesAsync(connection, id, ct));
    }

    private static (Guid Id, DateTimeOffset Start, DateTimeOffset? End) ReadHeader(SqliteDataReader reader) =>
        (Guid.Parse(reader.GetString(0)),
         Parse(reader.GetString(1)),
         reader.IsDBNull(2) ? null : Parse(reader.GetString(2)));

    private static async Task<List<LocationSample>> ReadSamplesAsync(SqliteConnection connection, Guid tripId, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Latitude, Longitude, Timestamp, SpeedMps, AccuracyM
            FROM Samples WHERE TripId = $id ORDER BY Seq
            """;
        cmd.Parameters.AddWithValue("$id", tripId.ToString());

        var samples = new List<LocationSample>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            samples.Add(new LocationSample(
                reader.GetDouble(0),
                reader.GetDouble(1),
                Parse(reader.GetString(2)),
                reader.IsDBNull(3) ? null : reader.GetDouble(3),
                reader.IsDBNull(4) ? null : reader.GetDouble(4)));
        }

        return samples;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            await pragma.ExecuteNonQueryAsync(ct);
        }

        if (!_initialized)
        {
            await _initGate.WaitAsync(ct);
            try
            {
                if (!_initialized)
                {
                    await using var schema = connection.CreateCommand();
                    schema.CommandText = Schema;
                    await schema.ExecuteNonQueryAsync(ct);
                    _initialized = true;
                }
            }
            finally
            {
                _initGate.Release();
            }
        }

        return connection;
    }

    private static string Format(DateTimeOffset value) => value.ToString("O");

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
