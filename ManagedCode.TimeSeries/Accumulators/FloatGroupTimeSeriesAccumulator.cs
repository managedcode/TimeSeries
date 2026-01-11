using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Grouped accumulator for floating-point events keyed by string.
/// </summary>
public sealed class FloatGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
    : BaseGroupTimeSeriesAccumulator<float, FloatTimeSeriesAccumulator>(sampleInterval, deleteOverdueSamples,
        () => new FloatTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
{
    /// <summary>
    /// Gets the maximum number of buckets to retain per key. Use 0 for unbounded.
    /// </summary>
    public int MaxSamplesCount { get; } = maxSamplesCount;
}
