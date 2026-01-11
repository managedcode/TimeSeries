using System.Numerics;
using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for generic numeric summers.
/// </summary>
[RegisterConverter]
public sealed class NumberTimeSeriesSummerConverter<TNumber> : IConverter<NumberTimeSeriesSummer<TNumber>, TimeSeriesSummerSurrogate<TNumber>>
    where TNumber : struct, INumber<TNumber>
{
    /// <inheritdoc />
    public NumberTimeSeriesSummer<TNumber> ConvertFromSurrogate(in TimeSeriesSummerSurrogate<TNumber> surrogate)
    {
        var series = new NumberTimeSeriesSummer<TNumber>(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = new Dictionary<DateTimeOffset, TNumber>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = pair.Value;
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesSummerSurrogate<TNumber> ConvertToSurrogate(in NumberTimeSeriesSummer<TNumber> value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, TNumber>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = pair.Value;
        }

        return new TimeSeriesSummerSurrogate<TNumber>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
