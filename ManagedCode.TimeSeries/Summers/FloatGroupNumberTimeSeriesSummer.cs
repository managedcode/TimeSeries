using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Grouped numeric summer for floating-point values.
/// </summary>
public class FloatGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples) : NumberGroupTimeSeriesSummer<float>(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
{
}
