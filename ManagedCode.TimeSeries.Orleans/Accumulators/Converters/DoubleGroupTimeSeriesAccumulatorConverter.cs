using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped double accumulators.
/// </summary>
[RegisterConverter]
public sealed class DoubleGroupTimeSeriesAccumulatorConverter : IConverter<DoubleGroupTimeSeriesAccumulator, TimeSeriesGroupAccumulatorsSurrogate<double>>
{
    /// <inheritdoc />
    public DoubleGroupTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesGroupAccumulatorsSurrogate<double> surrogate)
    {
        var group = new DoubleGroupTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new DoubleTimeSeriesAccumulatorConverter();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            var target = group.GetOrAdd(pair.Key);
            target.Merge(restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupAccumulatorsSurrogate<double> ConvertToSurrogate(in DoubleGroupTimeSeriesAccumulator value)
    {
        var snapshot = value.Snapshot();
        var series = new Dictionary<string, TimeSeriesAccumulatorsSurrogate<double>>(snapshot.Count);
        var converter = new DoubleTimeSeriesAccumulatorConverter();

        foreach (var pair in snapshot)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupAccumulatorsSurrogate<double>(series, value.SampleInterval, value.MaxSamplesCount, value.DeleteOverdueSamplesEnabled);
    }
}
