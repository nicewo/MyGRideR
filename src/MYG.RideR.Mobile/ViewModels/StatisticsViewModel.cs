using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Application.Abstractions;
using MYG.RideR.Application.Trips;

namespace MYG.RideR.Mobile.ViewModels;

public sealed partial class StatisticsViewModel : ObservableObject
{
    private readonly ITripRepository _repository;
    private readonly TimeProvider _timeProvider;

    public string Title => "Statistics";

    [ObservableProperty]
    public partial string TripCountText { get; private set; } = "0";

    [ObservableProperty]
    public partial string TotalDistanceText { get; private set; } = "0.0 km";

    [ObservableProperty]
    public partial string TotalDurationText { get; private set; } = "00:00:00";

    [ObservableProperty]
    public partial string AverageSpeedText { get; private set; } = "0 km/h";

    [ObservableProperty]
    public partial string MaximumSpeedText { get; private set; } = "0 km/h";

    public StatisticsViewModel(ITripRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        var stats = TripStatistics.Aggregate(await _repository.ListAsync(), _timeProvider.GetUtcNow());
        TripCountText = stats.TripCount.ToString();
        TotalDistanceText = $"{stats.TotalDistanceKm:0.0} km";
        TotalDurationText = $"{(int)stats.TotalDuration.TotalHours:00}:{stats.TotalDuration.Minutes:00}:{stats.TotalDuration.Seconds:00}";
        AverageSpeedText = $"{stats.AverageSpeedKmh:0} km/h";
        MaximumSpeedText = $"{stats.MaximumSpeedKmh:0} km/h";
    }
}
