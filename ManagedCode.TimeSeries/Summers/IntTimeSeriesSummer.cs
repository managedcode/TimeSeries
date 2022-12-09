using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class IntTimeSeriesSummer : BaseNumberTimeSeriesSummer<int, IntTimeSeriesSummer>
{
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount, strategy)
    {
    }

    public IntTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount) : base(sampleInterval, maxSamplesCount, Strategy.Sum)
    {
    }

    public IntTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }
}