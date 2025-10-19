using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new ConcurrentQueue<int>(kvp.Value));
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<int> ConvertToSurrogate(in IntTimeSeriesAccumulator value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new Queue<int>(kvp.Value));
        return new TimeSeriesAccumulatorsSurrogate<int>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
