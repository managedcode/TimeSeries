using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="IntTimeSeriesSummer"/>.
/// </summary>
[RegisterConverter]
public sealed class IntTimeSeriesSummerConverter : IConverter<IntTimeSeriesSummer, TimeSeriesSummerSurrogate<int>>
{
    /// <inheritdoc />
    public IntTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<int> surrogate)
    {
        var series = new IntTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = new Dictionary<DateTimeOffset, int>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = pair.Value;
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesSummerSurrogate<int> ConvertToSurrogate(in IntTimeSeriesSummer value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, int>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = pair.Value;
        }

        return new TimeSeriesSummerSurrogate<int>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
