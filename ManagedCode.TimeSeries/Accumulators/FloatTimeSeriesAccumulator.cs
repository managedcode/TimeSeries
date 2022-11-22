using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Accumulators;

public class FloatTimeSeriesAccumulator : BaseTimeSeriesAccumulator<float, FloatTimeSeriesAccumulator>
{
    public FloatTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }
}