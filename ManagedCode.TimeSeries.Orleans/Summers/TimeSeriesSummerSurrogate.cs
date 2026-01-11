using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans surrogate for time-series summers.
/// </summary>
// Surrogates should use plain fields instead of properties for better performance.
[Immutable]
[GenerateSerializer]
public struct TimeSeriesSummerSurrogate<T>(Dictionary<DateTimeOffset, T> samples,
    DateTimeOffset start,
    DateTimeOffset end,
    TimeSpan sampleInterval,
    int maxSamplesCount,
    DateTimeOffset lastDate,
    ulong dataCount,
    Strategy strategy)
{
    [Id(0)]
    public Dictionary<DateTimeOffset, T> Samples = samples;
    [Id(1)]
    public DateTimeOffset Start = start;
    [Id(2)]
    public DateTimeOffset End = end;
    [Id(3)]
    public TimeSpan SampleInterval = sampleInterval;
    [Id(4)]
    public int MaxSamplesCount = maxSamplesCount;
    [Id(5)]
    public DateTimeOffset LastDate = lastDate;
    [Id(6)]
    public ulong DataCount = dataCount;
    [Id(7)]
    public Strategy Strategy = strategy;

}
