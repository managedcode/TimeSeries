using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped float accumulators.
/// </summary>
[RegisterConverter]
public sealed class FloatGroupTimeSeriesAccumulatorConverter : IConverter<FloatGroupTimeSeriesAccumulator, TimeSeriesGroupAccumulatorsSurrogate<float>>
{
    /// <inheritdoc />
    public FloatGroupTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesGroupAccumulatorsSurrogate<float> surrogate)
    {
        var group = new FloatGroupTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new FloatTimeSeriesAccumulatorConverter();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            var target = group.GetOrAdd(pair.Key);
            target.Merge(restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupAccumulatorsSurrogate<float> ConvertToSurrogate(in FloatGroupTimeSeriesAccumulator value)
    {
        var snapshot = value.Snapshot();
        var series = new Dictionary<string, TimeSeriesAccumulatorsSurrogate<float>>(snapshot.Count);
        var converter = new FloatTimeSeriesAccumulatorConverter();

        foreach (var pair in snapshot)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupAccumulatorsSurrogate<float>(series, value.SampleInterval, value.MaxSamplesCount, value.DeleteOverdueSamplesEnabled);
    }
}
