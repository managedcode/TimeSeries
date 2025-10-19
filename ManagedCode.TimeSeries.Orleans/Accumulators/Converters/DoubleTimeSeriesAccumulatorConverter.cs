using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class DoubleTimeSeriesAccumulatorConverter<T> : IConverter<DoubleTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<double>>
{
    public DoubleTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new ConcurrentQueue<double>(kvp.Value));
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesAccumulator value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => new Queue<double>(kvp.Value));
        return new TimeSeriesAccumulatorsSurrogate<double>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}
