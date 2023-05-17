using ManagedCode.TimeSeries.Accumulators;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class DoubleTimeSeriesAccumulatorConverter<T> : IConverter<DoubleTimeSeriesAccumulator, TimeSeriesAccumulatorsSurrogate<double>>
{
    public DoubleTimeSeriesAccumulator ConvertFromSurrogate(in TimeSeriesAccumulatorsSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesAccumulator(surrogate.SampleInterval, surrogate.MaxSamplesCount);
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesAccumulatorsSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesAccumulator value)
    {
        return new TimeSeriesAccumulatorsSurrogate<double>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount);
    }
}