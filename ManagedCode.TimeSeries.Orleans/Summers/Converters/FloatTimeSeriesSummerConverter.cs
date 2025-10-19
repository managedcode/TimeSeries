using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ManagedCode.TimeSeries.Summers;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

[RegisterConverter]
public sealed class FloatTimeSeriesSummerConverter<T> : IConverter<FloatTimeSeriesSummer, TimeSeriesSummerSurrogate<float>>
{
    public FloatTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesSummerSurrogate<float> surrogate)
    {
        var series = new FloatTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy);
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<float> ConvertToSurrogate(in FloatTimeSeriesSummer value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        return new TimeSeriesSummerSurrogate<float>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
