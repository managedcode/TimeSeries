using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class IntGroupNumberTimeSeriesSummer : NumberGroupTimeSeriesSummer<int>
{
    public IntGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples)
        : base(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
    {
    }
}
