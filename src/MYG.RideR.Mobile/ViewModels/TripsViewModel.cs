using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;

namespace MYG.RideR.Mobile.ViewModels;

public sealed partial class TripsViewModel : ObservableObject
{
    private readonly ITripRepository _repository;
    private readonly TimeProvider _timeProvider;

    public string Title => "Trips";

    public ObservableCollection<TripListItem> Trips { get; } = [];

    public TripsViewModel(ITripRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var trips = await _repository.ListAsync();
        var now = _timeProvider.GetUtcNow();
        Trips.Clear();
        foreach (var trip in trips)
        {
            var summary = TripSummary.From(trip, now);
            Trips.Add(new TripListItem(
                trip.StartedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                $"{summary.DistanceKm:0.0} km · {(int)summary.Duration.TotalHours:00}:{summary.Duration.Minutes:00}:{summary.Duration.Seconds:00} · max {summary.MaximumSpeedKmh:0} km/h"));
        }
    }
}

public sealed record TripListItem(string Title, string Details);
