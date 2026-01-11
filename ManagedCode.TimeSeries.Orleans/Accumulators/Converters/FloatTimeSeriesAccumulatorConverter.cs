using System.Collections.Concurrent;
using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="FloatTimeSeriesAccumulator"/>.
/// </summary>
[RegisterConverter]
public sealed class FloatTimeSeriesAccumulatorConverter : IConverter<FloatTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<float>>
{
    /// <inheritdoc />
    public FloatTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<float>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = new ConcurrentQueue<float>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesAccumulatorsSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesAccumulator value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, Queue<float>>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = new Queue<float>(pair.Value);
        }

        return new TimeSeriesAccumulatorsSurrogate<float>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
