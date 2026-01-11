using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped integer summers.
/// </summary>
[RegisterConverter]
public sealed class IntGroupNumberTimeSeriesSummerConverter : IConverter<IntGroupNumberTimeSeriesSummer, TimeSeriesGroupSummerSurrogate<int>>
{
    /// <inheritdoc />
    public IntGroupNumberTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesGroupSummerSurrogate<int> surrogate)
    {
        var group = new IntGroupNumberTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new NumberTimeSeriesSummerConverter<int>();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            group.TimeSeries.TryAdd(pair.Key, restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupSummerSurrogate<int> ConvertToSurrogate(in IntGroupNumberTimeSeriesSummer value)
    {
        var series = new Dictionary<string, TimeSeriesSummerSurrogate<int>>(value.TimeSeries.Count);
        var converter = new NumberTimeSeriesSummerConverter<int>();

        foreach (var pair in value.TimeSeries)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupSummerSurrogate<int>(series, value.SampleInterval, value.MaxSamplesCount, value.Strategy, value.DeleteOverdueSamplesEnabled);
    }
}
