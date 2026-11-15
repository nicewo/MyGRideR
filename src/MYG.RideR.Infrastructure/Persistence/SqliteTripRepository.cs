using Microsoft.Data.Sqlite;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Infrastructure.Persistence;

public sealed class SqliteTripRepository : ITripRepository
{
    private const string Schema = """
        CREATE TABLE IF NOT EXISTS Trips (
            Id TEXT PRIMARY KEY,
            StartedAt TEXT NOT NULL,
            EndedAt TEXT NULL,
            DistanceM REAL NULL,
            MaxSpeedKmh REAL NULL,
            StoppedSeconds REAL NULL
        );
        CREATE TABLE IF NOT EXISTS Samples (
            TripId TEXT NOT NULL REFERENCES Trips(Id) ON DELETE CASCADE,
            Seq INTEGER NOT NULL,
            Latitude REAL NOT NULL,
            Longitude REAL NOT NULL,
            Timestamp TEXT NOT NULL,
            SpeedMps REAL NULL,
            AccuracyM REAL NULL,
            AltitudeM REAL NULL,
            CourseDeg REAL NULL,
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
            insert.CommandText = """
                INSERT INTO Trips (Id, StartedAt, EndedAt, DistanceM, MaxSpeedKmh, StoppedSeconds)
                VALUES ($id, $start, $end, $dist, $max, $stopped)
                """;
            insert.Parameters.AddWithValue("$id", trip.Id.ToString());
            insert.Parameters.AddWithValue("$start", Format(trip.StartedAt));
            insert.Parameters.AddWithValue("$end", trip.EndedAt is { } e ? Format(e) : DBNull.Value);
            if (trip.EndedAt is { } ended)
            {
                var summary = TripSummary.From(trip, ended);
                insert.Parameters.AddWithValue("$dist", summary.DistanceKm * 1000);
                insert.Parameters.AddWithValue("$max", summary.MaximumSpeedKmh);
                insert.Parameters.AddWithValue("$stopped", summary.StoppedDuration.TotalSeconds);
            }
            else
            {
                insert.Parameters.AddWithValue("$dist", DBNull.Value);
                insert.Parameters.AddWithValue("$max", DBNull.Value);
                insert.Parameters.AddWithValue("$stopped", DBNull.Value);
            }

            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await InsertSamplesAsync(connection, tx, trip.Id, 0, trip.Route.Samples, cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TripHeader>> ListHeadersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var rows = new List<(Guid Id, DateTimeOffset Start, DateTimeOffset? End, double? Dist, double? Max, double? Stopped)>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = """
                SELECT Id, StartedAt, EndedAt, DistanceM, MaxSpeedKmh, StoppedSeconds
                FROM Trips ORDER BY StartedAt DESC
                """;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var (id, start, end) = ReadHeader(reader);
                rows.Add((id, start, end,
                    reader.IsDBNull(3) ? null : reader.GetDouble(3),
                    reader.IsDBNull(4) ? null : reader.GetDouble(4),
                    reader.IsDBNull(5) ? null : reader.GetDouble(5)));
            }
        }

        var headers = new List<TripHeader>(rows.Count);
        foreach (var (id, start, end, dist, max, stopped) in rows)
        {
            if (dist is { } d && max is { } m && stopped is { } s)
            {
                headers.Add(new TripHeader(id, start, end, d / 1000, m, TimeSpan.FromSeconds(s)));
                continue;
            }

            // Summary columns missing (legacy finished row, or unfinished trip): compute from samples.
            var trip = Trip.Restore(id, start, end, await ReadSamplesAsync(connection, id, cancellationToken));
            var summary = TripSummary.From(trip, end ?? DateTimeOffset.UtcNow);
            headers.Add(new TripHeader(id, start, end, summary.DistanceKm, summary.MaximumSpeedKmh, summary.StoppedDuration));

            if (end is not null)
                await WriteSummaryAsync(connection, id, summary, cancellationToken);
        }

        return headers;
    }

    private static async Task WriteSummaryAsync(SqliteConnection connection, Guid id, TripSummary summary, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Trips SET DistanceM = $dist, MaxSpeedKmh = $max, StoppedSeconds = $stopped WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$dist", summary.DistanceKm * 1000);
        cmd.Parameters.AddWithValue("$max", summary.MaximumSpeedKmh);
        cmd.Parameters.AddWithValue("$stopped", summary.StoppedDuration.TotalSeconds);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertSamplesAsync(
        SqliteConnection connection, SqliteTransaction tx, Guid tripId, int firstSeq,
        IReadOnlyList<LocationSample> samples, CancellationToken ct)
    {
        if (samples.Count == 0)
            return;

        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO Samples (TripId, Seq, Latitude, Longitude, Timestamp, SpeedMps, AccuracyM, AltitudeM, CourseDeg)
            VALUES ($id, $seq, $lat, $lon, $ts, $speed, $acc, $alt, $course)
            """;
        var pId = cmd.Parameters.Add("$id", SqliteType.Text);
        var pSeq = cmd.Parameters.Add("$seq", SqliteType.Integer);
        var pLat = cmd.Parameters.Add("$lat", SqliteType.Real);
        var pLon = cmd.Parameters.Add("$lon", SqliteType.Real);
        var pTs = cmd.Parameters.Add("$ts", SqliteType.Text);
        var pSpeed = cmd.Parameters.Add("$speed", SqliteType.Real);
        var pAcc = cmd.Parameters.Add("$acc", SqliteType.Real);
        var pAlt = cmd.Parameters.Add("$alt", SqliteType.Real);
        var pCourse = cmd.Parameters.Add("$course", SqliteType.Real);
        cmd.Prepare();

        pId.Value = tripId.ToString();
        var seq = firstSeq;
        foreach (var s in samples)
        {
            pSeq.Value = seq++;
            pLat.Value = s.Latitude;
            pLon.Value = s.Longitude;
            pTs.Value = Format(s.Timestamp);
            pSpeed.Value = s.SpeedMetersPerSecond is { } sp ? sp : DBNull.Value;
            pAcc.Value = s.AccuracyMeters is { } a ? a : DBNull.Value;
            pAlt.Value = s.AltitudeMeters is { } al ? al : DBNull.Value;
            pCourse.Value = s.CourseDegrees is { } cd ? cd : DBNull.Value;
            await cmd.ExecuteNonQueryAsync(ct);
        }
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

    public async Task CreateAsync(Guid id, DateTimeOffset startedAt, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Trips (Id, StartedAt, EndedAt) VALUES ($id, $start, NULL)";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$start", Format(startedAt));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AppendSamplesAsync(
        Guid tripId, int firstSeq, IReadOnlyList<LocationSample> samples, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
            return;

        await using var connection = await OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await InsertSamplesAsync(connection, tx, tripId, firstSeq, samples, cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid tripId, DateTimeOffset endedAt, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);

        Trip? trip = null;
        DateTimeOffset? start = null;
        await using (var q = connection.CreateCommand())
        {
            q.CommandText = "SELECT StartedAt FROM Trips WHERE Id = $id";
            q.Parameters.AddWithValue("$id", tripId.ToString());
            if (await q.ExecuteScalarAsync(cancellationToken) is string s)
                start = Parse(s);
        }

        if (start is { } startedAt)
            trip = Trip.Restore(tripId, startedAt, endedAt, await ReadSamplesAsync(connection, tripId, cancellationToken));

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Trips SET EndedAt = $end, DistanceM = $dist, MaxSpeedKmh = $max, StoppedSeconds = $stopped WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", tripId.ToString());
        cmd.Parameters.AddWithValue("$end", Format(endedAt));
        var summary = trip is null ? null : TripSummary.From(trip, endedAt);
        cmd.Parameters.AddWithValue("$dist", summary is null ? DBNull.Value : summary.DistanceKm * 1000);
        cmd.Parameters.AddWithValue("$max", summary is null ? DBNull.Value : summary.MaximumSpeedKmh);
        cmd.Parameters.AddWithValue("$stopped", summary is null ? DBNull.Value : summary.StoppedDuration.TotalSeconds);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> ListUnfinishedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var headers = new List<(Guid Id, DateTimeOffset Start, DateTimeOffset? End)>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, StartedAt, EndedAt FROM Trips WHERE EndedAt IS NULL ORDER BY StartedAt";
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
            SELECT Latitude, Longitude, Timestamp, SpeedMps, AccuracyM, AltitudeM, CourseDeg
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
                reader.IsDBNull(4) ? null : reader.GetDouble(4),
                reader.IsDBNull(5) ? null : reader.GetDouble(5),
                reader.IsDBNull(6) ? null : reader.GetDouble(6)));
        }

        return samples;
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
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
                    await MigrateSamplesAsync(connection, ct);
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

    private static async Task MigrateSamplesAsync(SqliteConnection connection, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var info = connection.CreateCommand())
        {
            info.CommandText = "PRAGMA table_info(Samples)";
            await using var reader = await info.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                columns.Add(reader.GetString(1));
        }

        foreach (var column in new[] { "AltitudeM", "CourseDeg" })
        {
            if (columns.Contains(column))
                continue;

            await using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE Samples ADD COLUMN {column} REAL NULL";
            await alter.ExecuteNonQueryAsync(ct);
        }

        var tripColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var info = connection.CreateCommand())
        {
            info.CommandText = "PRAGMA table_info(Trips)";
            await using var reader = await info.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                tripColumns.Add(reader.GetString(1));
        }

        foreach (var column in new[] { "DistanceM", "MaxSpeedKmh", "StoppedSeconds" })
        {
            if (tripColumns.Contains(column))
                continue;

            await using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE Trips ADD COLUMN {column} REAL NULL";
            await alter.ExecuteNonQueryAsync(ct);
        }
    }

    private static string Format(DateTimeOffset value) => value.ToString("O");

    private static DateTimeOffset Parse(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
