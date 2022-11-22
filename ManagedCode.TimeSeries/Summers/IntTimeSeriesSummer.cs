using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class IntTimeSeriesSummer : BaseTimeSeriesSummer<int, IntTimeSeriesSummer>
{
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount, strategy)
    {
    }

    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount, Strategy.Sum)
    {
    }

    public IntTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }
}