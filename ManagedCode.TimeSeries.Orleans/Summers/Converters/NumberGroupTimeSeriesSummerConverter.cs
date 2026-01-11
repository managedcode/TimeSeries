using System.Numerics;
using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped generic numeric summers.
/// </summary>
[RegisterConverter]
public sealed class NumberGroupTimeSeriesSummerConverter<TNumber> : IConverter<NumberGroupTimeSeriesSummer<TNumber>, TimeSeriesGroupSummerSurrogate<TNumber>>
    where TNumber : struct, INumber<TNumber>
{
    /// <inheritdoc />
    public NumberGroupTimeSeriesSummer<TNumber> ConvertFromSurrogate(in TimeSeriesGroupSummerSurrogate<TNumber> surrogate)
    {
        var group = new NumberGroupTimeSeriesSummer<TNumber>(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new NumberTimeSeriesSummerConverter<TNumber>();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            group.TimeSeries.TryAdd(pair.Key, restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupSummerSurrogate<TNumber> ConvertToSurrogate(in NumberGroupTimeSeriesSummer<TNumber> value)
    {
        var series = new Dictionary<string, TimeSeriesSummerSurrogate<TNumber>>(value.TimeSeries.Count);
        var converter = new NumberTimeSeriesSummerConverter<TNumber>();

        foreach (var pair in value.TimeSeries)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupSummerSurrogate<TNumber>(series, value.SampleInterval, value.MaxSamplesCount, value.Strategy, value.DeleteOverdueSamplesEnabled);
    }
}
