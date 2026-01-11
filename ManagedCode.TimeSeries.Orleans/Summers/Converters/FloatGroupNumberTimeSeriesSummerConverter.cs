using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped float summers.
/// </summary>
[RegisterConverter]
public sealed class FloatGroupNumberTimeSeriesSummerConverter : IConverter<FloatGroupNumberTimeSeriesSummer, TimeSeriesGroupSummerSurrogate<float>>
{
    /// <inheritdoc />
    public FloatGroupNumberTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesGroupSummerSurrogate<float> surrogate)
    {
        var group = new FloatGroupNumberTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new NumberTimeSeriesSummerConverter<float>();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            group.TimeSeries.TryAdd(pair.Key, restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupSummerSurrogate<float> ConvertToSurrogate(in FloatGroupNumberTimeSeriesSummer value)
    {
        var series = new Dictionary<string, TimeSeriesSummerSurrogate<float>>(value.TimeSeries.Count);
        var converter = new NumberTimeSeriesSummerConverter<float>();

        foreach (var pair in value.TimeSeries)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupSummerSurrogate<float>(series, value.SampleInterval, value.MaxSamplesCount, value.Strategy, value.DeleteOverdueSamplesEnabled);
    }
}
