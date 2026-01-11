using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Accumulator for integer events stored in per-bucket queues.
/// </summary>
public class IntTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0)
    : BaseTimeSeriesAccumulator<int, IntTimeSeriesAccumulator>(sampleInterval, maxSamplesCount);
