using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class FloatGroupNumberTimeSeriesSummer : NumberGroupTimeSeriesSummer<float>
{
    public FloatGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples)
        : base(sampleInterval, samplesCount, strategy, deleteOverdueSamples)
    {
    }
}
