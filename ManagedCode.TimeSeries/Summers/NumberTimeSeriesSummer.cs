using System.Numerics;
using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public sealed class NumberTimeSeriesSummer<TNumber> : BaseNumberTimeSeriesSummer<TNumber, NumberTimeSeriesSummer<TNumber>>
    where TNumber : struct, INumber<TNumber>
{
    public NumberTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy)
        : base(sampleInterval, maxSamplesCount, strategy)
    {
    }

    public NumberTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount)
        : this(sampleInterval, maxSamplesCount, Strategy.Sum)
    {
    }

    public NumberTimeSeriesSummer(TimeSpan sampleInterval)
        : this(sampleInterval, 0, Strategy.Sum)
    {
    }
}
