using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class DoubleTimeSeriesSummer : BaseTimeSeriesSummer<double, DoubleTimeSeriesSummer>
{
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount, strategy)
    {
    }

    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount) : base(sampleInterval, maxSamplesCount, Strategy.Sum)
    {
    }

    public DoubleTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }
}