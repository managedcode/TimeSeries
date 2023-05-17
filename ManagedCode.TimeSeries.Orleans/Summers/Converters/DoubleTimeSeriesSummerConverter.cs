using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class DoubleTimeSeriesSummerConverter<T> : IConverter<DoubleTimeSeriesSummer, TimeSeriesSummerSurrogate<double>>
{
    public DoubleTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        series.InitInternal(surrogate.Samples, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesSummer value)
    {
        return new TimeSeriesSummerSurrogate<double>(value.Samples, value.Start, value.End, 
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
