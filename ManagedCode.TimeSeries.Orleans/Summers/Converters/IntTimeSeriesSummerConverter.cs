using System;
using System.Collections.Generic;
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
        var converted = new Dictionary<DateTimeOffset, int>(surrogate.Samples.Count);

        foreach (var pair in surrogate.Samples)
        {
            converted[pair.Key] = pair.Value;
        }

        series.InitInternal(converted, surrogate.Start, surrogate.End, surrogate.LastDate, surrogate.DataCount);
        return series;
    }

    public TimeSeriesSummerSurrogate<int> ConvertToSurrogate(in IntTimeSeriesSummer value)
    {
        var samples = value.Samples;
        var converted = new Dictionary<DateTimeOffset, int>(samples.Count);

        foreach (var pair in samples)
        {
            converted[pair.Key] = pair.Value;
        }

        return new TimeSeriesSummerSurrogate<int>(converted, value.Start, value.End,
            value.SampleInterval, value.MaxSamplesCount, value.LastDate, value.DataCount, value.Strategy);
    }
}
