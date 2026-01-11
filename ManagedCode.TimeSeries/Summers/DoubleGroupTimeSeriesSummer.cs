using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Grouped numeric summer for double-precision values.
/// </summary>
public class DoubleGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples) : NumberGroupTimeSeriesSummer<double>(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
{
}
