using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class FloatTimeSeriesAccumulatorConverter<T> : IConverter<FloatTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<float>>
{
    public FloatTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new ConcurrentQueue<float>(kvp.Value));
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesAccumulator value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new Queue<float>(kvp.Value));
        return new TimeSeriesAccumulatorsSurrogate<float>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
