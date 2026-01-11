using System.Numerics;
using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Numeric summer for any <see cref="INumber{T}"/> type.
/// </summary>
public sealed class NumberTimeSeriesSummer<TNumber>(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : BaseNumberTimeSeriesSummer<TNumber, NumberTimeSeriesSummer<TNumber>>(sampleInterval, maxSamplesCount, strategy)
    where TNumber : struct, INumber<TNumber>
{
    public NumberTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount)
        : this(sampleInterval, maxSamplesCount, Strategy.Sum)
    {
    }

    public NumberTimeSeriesSummer(TimeSpan sampleInterval)
        : this(sampleInterval, 0, Strategy.Sum)
    {
    }
}
