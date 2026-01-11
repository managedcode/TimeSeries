using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Grouped accumulator for double-precision events keyed by string.
/// </summary>
public sealed class DoubleGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
    : BaseGroupTimeSeriesAccumulator<double, DoubleTimeSeriesAccumulator>(sampleInterval, deleteOverdueSamples,
        () => new DoubleTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
{
    /// <summary>
    /// Gets the maximum number of buckets to retain per key. Use 0 for unbounded.
    /// </summary>
    public int MaxSamplesCount { get; } = maxSamplesCount;
}
