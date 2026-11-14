using MYG.RideR.Application.Abstractions;
using MYG.RideR.Core.Domain;

namespace MYG.RideR.Application.Trips;

public sealed class TripRecorder : ITripRecorder
{
    private readonly ILocationService _locationService;
    private readonly ITripRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();

    public TripRecorder(
        ILocationService locationService,
        ITripRepository repository,
        TimeProvider timeProvider)
    {
        _locationService = locationService;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Trip? CurrentTrip { get; private set; }

    public async Task<Trip> StartAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentTrip is { Status: TripStatus.Recording })
            throw new InvalidOperationException("A trip is already being recorded.");

        var trip = Trip.Start(_timeProvider.GetUtcNow());
        CurrentTrip = trip;
        _locationService.LocationChanged += OnLocationChanged;

        if (!await _locationService.StartAsync(cancellationToken))
        {
            _locationService.LocationChanged -= OnLocationChanged;
            CurrentTrip = null;
            throw new InvalidOperationException("Location service could not be started.");
        }

        return trip;
    }

    public async Task<Trip> StopAsync(CancellationToken cancellationToken = default)
    {
        var trip = CurrentTrip ?? throw new InvalidOperationException("No trip is being recorded.");

        _locationService.LocationChanged -= OnLocationChanged;
        await _locationService.StopAsync(cancellationToken);

        lock (_gate)
        {
            trip.Complete(_timeProvider.GetUtcNow());
        }

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
