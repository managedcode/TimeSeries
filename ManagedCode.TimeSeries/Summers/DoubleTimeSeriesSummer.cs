using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class DoubleTimeSeriesSummer : BaseTimeSeriesSummer<double, DoubleTimeSeriesSummer>
{
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount, strategy)
    {
    }

    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount, Strategy.Sum)
    {
    }

    public DoubleTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }
}