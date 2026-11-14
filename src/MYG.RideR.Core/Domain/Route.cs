namespace MYG.RideR.Core.Domain;

public sealed class Route
{
    private readonly List<LocationSample> _samples = [];

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

            DistanceMeters += last.DistanceMetersTo(sample);
        }

        _samples.Add(sample);
    }
}
