using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class DoubleTimeSeriesSummerConverter<T> : IConverter<DoubleTimeSeriesSummer, TimeSeriesSummerSurrogate<double>>
{
    public DoubleTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<double> surrogate)
    {
        var series = new DoubleTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<double> ConvertToSurrogate(in DoubleTimeSeriesSummer value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        return new TimeSeriesSummerSurrogate<double>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
