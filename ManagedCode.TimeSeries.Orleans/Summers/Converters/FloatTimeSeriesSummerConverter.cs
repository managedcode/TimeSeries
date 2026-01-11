using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="FloatTimeSeriesSummer"/>.
/// </summary>
[RegisterConverter]
public sealed class FloatTimeSeriesSummerConverter : IConverter<FloatTimeSeriesSummer, TimeSeriesSummerSurrogate<float>>
{
    /// <inheritdoc />
    public FloatTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = new Dictionary<DateTimeOffset, float>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = pair.Value;
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesSummerSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesSummer value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, float>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = pair.Value;
        }

        return new TimeSeriesSummerSurrogate<float>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
