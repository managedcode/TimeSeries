using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class IntTimeSeriesAccumulatorConverter<T> : IConverter<IntTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<int>>
{
    public IntTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<int> surrogate)
    {
        var series = new IntTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<int>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            converted[pair.Key] = new ConcurrentQueue<int>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

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
