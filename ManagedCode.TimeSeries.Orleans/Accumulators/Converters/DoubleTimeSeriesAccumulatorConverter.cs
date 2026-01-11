using System.Collections.Concurrent;
using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="DoubleTimeSeriesAccumulator"/>.
/// </summary>
[RegisterConverter]
public sealed class DoubleTimeSeriesAccumulatorConverter : IConverter<DoubleTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<double>>
{
    /// <inheritdoc />
    public DoubleTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<double>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = new ConcurrentQueue<double>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesAccumulatorsSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesAccumulator value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, Queue<double>>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = new Queue<double>(pair.Value);
        }

        return new TimeSeriesAccumulatorsSurrogate<double>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
