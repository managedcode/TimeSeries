using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Accumulator for double-precision events stored in per-bucket queues.
/// </summary>
public class DoubleTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0)
    : BaseTimeSeriesAccumulator<double, DoubleTimeSeriesAccumulator>(sampleInterval, maxSamplesCount);
