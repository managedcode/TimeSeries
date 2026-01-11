namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans surrogate for grouped accumulators.
/// </summary>
// Surrogates should use plain fields instead of properties for better performance.
[Immutable]
[GenerateSerializer]
public struct TimeSeriesGroupAccumulatorsSurrogate<T>(Dictionary<string, TimeSeriesAccumulatorsSurrogate<T>> series,
    TimeSpan sampleInterval,
    int maxSamplesCount,
    bool deleteOverdueSamples)
{
    [Id(0)]
    public Dictionary<string, TimeSeriesAccumulatorsSurrogate<T>> Series = series;
    [Id(1)]
    public TimeSpan SampleInterval = sampleInterval;
    [Id(2)]
    public int MaxSamplesCount = maxSamplesCount;
    [Id(3)]
    public bool DeleteOverdueSamples = deleteOverdueSamples;
}
