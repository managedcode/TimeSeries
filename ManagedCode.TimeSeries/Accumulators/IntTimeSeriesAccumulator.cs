namespace ManagedCode.TimeSeries.Accumulators;

public class IntTimeSeriesAccumulator : BaseTimeSeriesAccumulator<int>
{
    public IntTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }
}