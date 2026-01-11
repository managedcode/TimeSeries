using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Numeric summer for double-precision values.
/// </summary>
public class DoubleTimeSeriesSummer : BaseNumberTimeSeriesSummer<double, DoubleTimeSeriesSummer>
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
