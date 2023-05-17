using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class FloatTimeSeriesSummerConverter<T> : IConverter<FloatTimeSeriesSummer, TimeSeriesSummerSurrogate<float>>
{
    public FloatTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesSummer value)
    {
        return new TimeSeriesSummerSurrogate<float>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}