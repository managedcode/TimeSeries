namespace ManagedCode.TimeSeries.Accumulators;

public class DoubleTimeSeriesAccumulator : BaseTimeSeriesAccumulator<double>
{
    public DoubleTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }
}