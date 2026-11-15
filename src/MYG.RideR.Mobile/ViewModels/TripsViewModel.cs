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
        var headers = await _repository.ListHeadersAsync();
        var now = _timeProvider.GetUtcNow();
        Trips.Clear();
        foreach (var h in headers)
        {
            var duration = h.Duration(now);
            Trips.Add(new TripListItem(
                h.StartedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                $"{h.DistanceKm:0.0} km · {(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00} · max {h.MaximumSpeedKmh:0} km/h"));
        }
    }
}

public sealed record TripListItem(string Title, string Details);
