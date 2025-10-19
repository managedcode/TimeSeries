using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public sealed class FloatGroupTimeSeriesAccumulator : BaseGroupTimeSeriesAccumulator<float, FloatTimeSeriesAccumulator>
{
    public FloatGroupTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0, bool deleteOverdueSamples = true)
        : base(sampleInterval, deleteOverdueSamples, () => new FloatTimeSeriesAccumulator(sampleInterval, maxSamplesCount))
    {
    }
}
