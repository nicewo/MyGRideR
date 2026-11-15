using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed class TripRecorder : ITripRecorder
{
    private const int FlushSampleThreshold = 15;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(15);

    private readonly ILiveLocationFeed _liveFeed;
    private readonly ITripRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private readonly IRecordingKeepAlive _keepAlive;
    private bool _feedHeld;
    private bool _keepAliveOn;
    private bool _created;
    private int _persistedCount;
    private DateTimeOffset _lastFlushAt;

    public TripRecorder(
        ILiveLocationFeed liveFeed,
        ITripRepository repository,
        TimeProvider timeProvider,
        IRecordingKeepAlive keepAlive)
    {
        _keepAlive = keepAlive;
        _liveFeed = liveFeed;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Trip? CurrentTrip { get; private set; }

    public TripSummary? GetCurrentSummary(DateTimeOffset now)
    {
        lock (_gate)
        {
            return CurrentTrip is { } t ? TripSummary.From(t, now) : null;
        }
    }

    public async Task<Trip> StartAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTrip is { Status: TripStatus.Recording })
            throw new InvalidOperationException("A trip is already being recorded.");
        if (CurrentTrip is { Status: TripStatus.Completed })
            throw new InvalidOperationException("Önceki sürüş henüz kaydedilmedi.");

        var now = _timeProvider.GetUtcNow();
        var trip = Trip.Start(now);
        lock (_gate)
        {
            _created = false;
            _persistedCount = 0;
            _lastFlushAt = now;
            CurrentTrip = trip;
        }

        _liveFeed.SampleReceived += OnLocationChanged;

        bool acquired;
        try
        {
            acquired = await _liveFeed.AcquireAsync(cancellationToken);
        }
        catch
        {
            _liveFeed.SampleReceived -= OnLocationChanged;
            CurrentTrip = null;
            throw;
        }

        if (!acquired)
        {
            _liveFeed.SampleReceived -= OnLocationChanged;
            CurrentTrip = null;
            throw new InvalidOperationException("Location service could not be started.");
        }

        _feedHeld = true;

        try
        {
            await _repository.CreateAsync(trip.Id, trip.StartedAt, cancellationToken);
        }
        catch
        {
            _liveFeed.SampleReceived -= OnLocationChanged;
            _feedHeld = false;
            try
            {
                await _liveFeed.ReleaseAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            CurrentTrip = null;
            throw;
        }

        lock (_gate)
        {
            _created = true;
        }

        try
        {
            await _keepAlive.StartAsync(CancellationToken.None);
            _keepAliveOn = true;
        }
        catch (Exception ex)
        {
            // Recording continues without the foreground service.
            System.Diagnostics.Debug.WriteLine(ex);
        }

        return trip;
    }

    public async Task<Trip> StopAsync(CancellationToken cancellationToken = default)
    {
        var trip = CurrentTrip ?? throw new InvalidOperationException("No trip is being recorded.");

        if (_feedHeld)
        {
            _liveFeed.SampleReceived -= OnLocationChanged;
            await _liveFeed.ReleaseAsync(cancellationToken);
            _feedHeld = false;
        }

        if (_keepAliveOn)
        {
            _keepAliveOn = false;
            try
            {
                await _keepAlive.StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        lock (_gate)
        {
            if (trip.Status == TripStatus.Recording)
                trip.Complete(_timeProvider.GetUtcNow());
        }

        // Failure keeps the completed trip so the next Stop retries only the persistence.
        await FlushAsync(throwOnError: true, cancellationToken);
        await _repository.CompleteAsync(trip.Id, trip.EndedAt!.Value, cancellationToken);
        CurrentTrip = null;
        return trip;
    }

    public async Task<int> RecoverUnfinishedAsync(CancellationToken cancellationToken = default)
    {
        var unfinished = await _repository.ListUnfinishedAsync(cancellationToken);
        var closed = 0;
        foreach (var trip in unfinished)
        {
            if (CurrentTrip is { } current && current.Id == trip.Id)
                continue;

            var samples = trip.Route.Samples;
            var endedAt = samples.Count > 0 ? samples[^1].Timestamp : trip.StartedAt;
            await _repository.CompleteAsync(trip.Id, endedAt, cancellationToken);
            closed++;
        }

        return closed;
    }

    private void OnLocationChanged(LocationSample sample)
    {
        var flush = false;
        lock (_gate)
        {
            if (CurrentTrip is not { Status: TripStatus.Recording } trip)
                return;

            try
            {
                trip.AddSample(sample);
            }
            catch (ArgumentException)
            {
                // Out-of-order sample from the platform; ignore it.
                return;
            }

            flush = _created
                && (trip.Route.Samples.Count - _persistedCount >= FlushSampleThreshold
                    || _timeProvider.GetUtcNow() - _lastFlushAt >= FlushInterval);
        }

        if (flush)
            _ = FlushAsync(throwOnError: false, CancellationToken.None);
    }

    private async Task FlushAsync(bool throwOnError, CancellationToken cancellationToken)
    {
        try
        {
            await _flushGate.WaitAsync(cancellationToken);
            try
            {
                Trip? trip;
                int first;
                LocationSample[] pending;
                lock (_gate)
                {
                    trip = CurrentTrip;
                    if (trip is null || !_created)
                        return;

                    first = _persistedCount;
                    var all = trip.Route.Samples;
                    pending = first < all.Count ? all.Skip(first).ToArray() : [];
                    _lastFlushAt = _timeProvider.GetUtcNow();
                }

                if (pending.Length == 0)
                    return;

                await _repository.AppendSamplesAsync(trip.Id, first, pending, cancellationToken);

                lock (_gate)
                {
                    _persistedCount = first + pending.Length;
                }
            }
            finally
            {
                _flushGate.Release();
            }
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw;

            // Samples stay in memory; the next flush retries them.
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}
