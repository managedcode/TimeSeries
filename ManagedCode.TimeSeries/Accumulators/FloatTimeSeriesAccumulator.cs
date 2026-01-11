using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

/// <summary>
/// Accumulator for floating-point events stored in per-bucket queues.
/// </summary>
public class FloatTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0)
    : BaseTimeSeriesAccumulator<float, FloatTimeSeriesAccumulator>(sampleInterval, maxSamplesCount);
