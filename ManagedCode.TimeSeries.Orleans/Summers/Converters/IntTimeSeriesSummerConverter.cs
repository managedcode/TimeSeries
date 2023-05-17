using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

// This is a converter which converts between the surrogate and the foreign type.
[RegisterConverter]
public sealed class IntTimeSeriesSummerConverter<T> : IConverter<IntTimeSeriesSummer, TimeSeriesSummerSurrogate<int>>
{
    public IntTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<int> surrogate)
    {
        var series = new IntTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<int> ConvertToSurrogate(in IntTimeSeriesSummer value)
    {
        return new TimeSeriesSummerSurrogate<int>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}