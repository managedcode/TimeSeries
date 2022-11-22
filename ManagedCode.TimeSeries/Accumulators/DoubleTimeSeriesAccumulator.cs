using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public class DoubleTimeSeriesAccumulator : BaseTimeSeriesAccumulator<double, DoubleTimeSeriesAccumulator>
{
    public DoubleTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount = 0) : base(sampleInterval, maxSamplesCount)
    {
    }
}