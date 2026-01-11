using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped integer accumulators.
/// </summary>
[RegisterConverter]
public sealed class IntGroupTimeSeriesAccumulatorConverter : IConverter<IntGroupTimeSeriesAccumulator, TimeSeriesGroupAccumulatorsSurrogate<int>>
{
    /// <inheritdoc />
    public IntGroupTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesGroupAccumulatorsSurrogate<int> surrogate)
    {
        var group = new IntGroupTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new IntTimeSeriesAccumulatorConverter();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            var target = group.GetOrAdd(pair.Key);
            target.Merge(restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupAccumulatorsSurrogate<int> ConvertToSurrogate(in IntGroupTimeSeriesAccumulator value)
    {
        var snapshot = value.Snapshot();
        var series = new Dictionary<string, TimeSeriesAccumulatorsSurrogate<int>>(snapshot.Count);
        var converter = new IntTimeSeriesAccumulatorConverter();

        foreach (var pair in snapshot)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupAccumulatorsSurrogate<int>(series, value.SampleInterval, value.MaxSamplesCount, value.DeleteOverdueSamplesEnabled);
    }
}
