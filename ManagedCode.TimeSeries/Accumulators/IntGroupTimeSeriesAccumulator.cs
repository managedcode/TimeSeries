using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Grouped accumulator for integer events keyed by string.
/// </summary>
public sealed class IntGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
    : BaseGroupTimeSeriesAccumulator<int, IntTimeSeriesAccumulator>(sampleInterval, deleteOverdueSamples,
        () => new IntTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
{
    /// <summary>
    /// Gets the maximum number of buckets to retain per key. Use 0 for unbounded.
    /// </summary>
    public int MaxSamplesCount { get; } = maxSamplesCount;
}
