using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public sealed class DoubleGroupTimeSeriesAccumulator : BaseGroupTimeSeriesAccumulator<double, DoubleTimeSeriesAccumulator>
{
    public DoubleGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
        : base(sampleInterval, deleteOverdueSamples, () => new DoubleTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
    {
    }
}
