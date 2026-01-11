using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans surrogate for grouped summers.
/// </summary>
// Surrogates should use plain fields instead of properties for better performance.
[Immutable]
[GenerateSerializer]
public struct TimeSeriesGroupSummerSurrogate<T>(Dictionary<string, TimeSeriesSummerSurrogate<T>> series,
    TimeSpan sampleInterval,
    int maxSamplesCount,
    Strategy strategy,
    bool deleteOverdueSamples)
{
    [Id(0)]
    public Dictionary<string, TimeSeriesSummerSurrogate<T>> Series = series;
    [Id(1)]
    public TimeSpan SampleInterval = sampleInterval;
    [Id(2)]
    public int MaxSamplesCount = maxSamplesCount;
    [Id(3)]
    public Strategy Strategy = strategy;
    [Id(4)]
    public bool DeleteOverdueSamples = deleteOverdueSamples;
}
