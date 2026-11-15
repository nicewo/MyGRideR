namespace MYG.RideR.Core.Domain;

public sealed class Route
{
    private readonly List<LocationSample> _samples = [];
    private LocationSample? _anchor;
    private int _rejectedJumps;

    public IReadOnlyList<LocationSample> Samples => _samples;

    public double DistanceMeters { get; private set; }

    public void Add(LocationSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        if (_samples.Count > 0)
        {
            var last = _samples[^1];
            if (sample.Timestamp < last.Timestamp)
                throw new ArgumentException("Sample is older than the last recorded sample.", nameof(sample));

        }

        _samples.Add(sample);
        UpdateDistance(sample);
    }

    private void UpdateDistance(LocationSample sample)
    {
        if (sample.AccuracyMeters is { } acc && acc > TrackingThresholds.MaxAccuracyMeters)
            return;

        if (_anchor is null)
        {
            _anchor = sample;
            return;
        }

        var d = _anchor.DistanceMetersTo(sample);
        var dt = (sample.Timestamp - _anchor.Timestamp).TotalSeconds;

        var minSegment = Math.Max(
            TrackingThresholds.MinSegmentMeters,
            Math.Max(_anchor.AccuracyMeters ?? 0, sample.AccuracyMeters ?? 0));
        if (d < minSegment)
            return;

        if (dt > 0 && d / dt > TrackingThresholds.MaxPlausibleSpeedMps)
        {
            _rejectedJumps++;
            if (_rejectedJumps >= TrackingThresholds.JumpReanchorCount)
            {
                _anchor = sample;
                _rejectedJumps = 0;
            }
            return;
        }

        DistanceMeters += d;
        _anchor = sample;
        _rejectedJumps = 0;
    }
}
