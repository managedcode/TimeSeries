using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="DoubleTimeSeriesSummer"/>.
/// </summary>
[RegisterConverter]
public sealed class DoubleTimeSeriesSummerConverter : IConverter<DoubleTimeSeriesSummer, TimeSeriesSummerSurrogate<double>>
{
    /// <inheritdoc />
    public DoubleTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = new Dictionary<DateTimeOffset, double>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = pair.Value;
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesSummerSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesSummer value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, double>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = pair.Value;
        }

        return new TimeSeriesSummerSurrogate<double>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
