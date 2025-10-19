using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public sealed class IntGroupTimeSeriesAccumulator : BaseGroupTimeSeriesAccumulator<int, IntTimeSeriesAccumulator>
{
    public IntGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
        : base(sampleInterval, deleteOverdueSamples, () => new IntTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
    {
    }
}
