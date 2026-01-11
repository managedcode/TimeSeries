using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Grouped numeric summer for integer values.
/// </summary>
public class IntGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples) : NumberGroupTimeSeriesSummer<int>(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
{
}
