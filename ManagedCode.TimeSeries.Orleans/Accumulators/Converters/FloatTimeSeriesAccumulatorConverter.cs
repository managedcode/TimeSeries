using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class FloatTimeSeriesAccumulatorConverter<T> : IConverter<FloatTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<float>>
{
    public FloatTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesAccumulator value)
    {
        return new TimeSeriesAccumulatorsSurrogate<float>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}