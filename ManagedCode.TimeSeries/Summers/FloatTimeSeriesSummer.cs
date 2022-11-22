using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class FloatTimeSeriesSummer : BaseTimeSeriesSummer<float, FloatTimeSeriesSummer>
{
    public FloatTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount, strategy)
    {
    }

    public FloatTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount) : base(sampleInterval, maxSamplesCount, Strategy.Sum)
    {
    }

    public FloatTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }
}