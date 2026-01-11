using System.Collections.Concurrent;
using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for <see cref="IntTimeSeriesAccumulator"/>.
/// </summary>
[RegisterConverter]
public sealed class IntTimeSeriesAccumulatorConverter : IConverter<IntTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<int>>
{
    /// <inheritdoc />
    public IntTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<int> surrogate)
    {
        var series = new IntTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<int>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            var normalizedKey = new DateTimeOffset(pair.Key.UtcDateTime, TimeSpan.Zero);
            converted[normalizedKey] = new ConcurrentQueue<int>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    /// <inheritdoc />
    public TimeSeriesAccumulatorsSurrogate<int> ConvertToSurrogate(in IntTimeSeriesAccumulator value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, Queue<int>>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = new Queue<int>(pair.Value);
        }

        return new TimeSeriesAccumulatorsSurrogate<int>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
