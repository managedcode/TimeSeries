using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class FloatTimeSeriesAccumulatorConverter<T> : IConverter<FloatTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<float>>
{
    public FloatTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<float>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            converted[pair.Key] = new ConcurrentQueue<float>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

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
