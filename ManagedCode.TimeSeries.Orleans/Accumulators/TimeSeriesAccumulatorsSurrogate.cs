using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using ManagedCode.TimeSeries.Abstractions;
using Orleans;

namespace ManagedCode.TimeSeries.Orleans;

// This is the surrogate which will act as a stand-in for the foreign type.
// Surrogates should use plain fields instead of properties for better perfomance.
[Immutable]
[GenerateSerializer]
public struct TimeSeriesAccumulatorsSurrogate<T>
{
    public TimeSeriesAccumulatorsSurrogate(Dictionary<DateTimeOffset, Queue<T>> samples,
        DateTimeOffset start,
        DateTimeOffset end,
        TimeSpan sampleInterval,
        int maxSamplesCount,
        DateTimeOffset lastDate,
        ulong dataCount)
    {
        Samples = samples;
        Start = start;
        End = end;
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;
        LastDate = lastDate;
        DataCount = dataCount;
    }

    [Id(0)]
    public Dictionary<DateTimeOffset, Queue<T>> Samples;
    [Id(1)]
    public DateTimeOffset Start;
    [Id(2)]
    public DateTimeOffset End;
    [Id(3)]
    public TimeSpan SampleInterval;
    [Id(4)]
    public int MaxSamplesCount;
    [Id(5)]
    public DateTimeOffset LastDate;
    [Id(6)]
    public ulong DataCount;
}