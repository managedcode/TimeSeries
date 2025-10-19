using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class DoubleTimeSeriesAccumulatorConverter<T> : IConverter<DoubleTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<double>>
{
    public DoubleTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<double>>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            converted[pair.Key] = new ConcurrentQueue<double>(pair.Value);
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

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
