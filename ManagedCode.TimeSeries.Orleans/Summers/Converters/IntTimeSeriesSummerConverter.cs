using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        var converted = surrogate.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<int> ConvertToSurrogate(in IntTimeSeriesSummer value)
    {
        var converted = value.Samples.ToDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value);
        return new TimeSeriesSummerSurrogate<int>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
