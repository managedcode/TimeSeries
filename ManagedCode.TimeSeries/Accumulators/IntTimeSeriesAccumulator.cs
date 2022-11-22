using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public class IntTimeSeriesAccumulator : BaseTimeSeriesAccumulator<int, IntTimeSeriesAccumulator>
{
    public IntTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }
}