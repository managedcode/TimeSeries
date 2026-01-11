namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans surrogate for time-series accumulators.
/// </summary>
// Surrogates should use plain fields instead of properties for better performance.
[Immutable]
[GenerateSerializer]
public struct TimeSeriesAccumulatorsSurrogate<T>(Dictionary<DateTimeOffset, Queue<T>> samples,
    DateTimeOffset start,
    DateTimeOffset end,
    TimeSpan sampleInterval,
    int maxSamplesCount,
    DateTimeOffset lastDate,
    ulong dataCount)
{
    [Id(0)]
    public Dictionary<DateTimeOffset, Queue<T>> Samples = samples;
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
}
