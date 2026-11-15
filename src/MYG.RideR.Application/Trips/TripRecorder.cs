using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed class TripRecorder : ITripRecorder
{
    private readonly ILiveLocationFeed _liveFeed;
    private readonly ITripRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private bool _feedHeld;

    public TripRecorder(
        ILiveLocationFeed liveFeed,
        ITripRepository repository,
        TimeProvider timeProvider)
    {
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

        var trip = Trip.Start(_timeProvider.GetUtcNow());
        CurrentTrip = trip;
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

        lock (_gate)
        {
            if (trip.Status == TripStatus.Recording)
                trip.Complete(_timeProvider.GetUtcNow());
        }

        // Save failure keeps the completed trip so the next Stop retries only the save.
        await _repository.SaveAsync(trip, cancellationToken);
        CurrentTrip = null;
        return trip;
    }

    private void OnLocationChanged(LocationSample sample)
    {
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
            }
        }
    }
}
