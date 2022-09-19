namespace ManagedCode.TimeSeries.Accumulators;

public class FloatTimeSeriesAccumulator : BaseTimeSeriesAccumulator<float>
{
    public FloatTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }
}