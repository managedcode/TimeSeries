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
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<int> ConvertToSurrogate(in IntTimeSeriesAccumulator value)
    {
        return new TimeSeriesAccumulatorsSurrogate<int>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}