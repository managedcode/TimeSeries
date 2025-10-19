using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class DoubleGroupTimeSeriesSummer : NumberGroupTimeSeriesSummer<double>
{
    public DoubleGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples)
        : base(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
    {
    }
}
