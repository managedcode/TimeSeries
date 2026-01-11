using System.Numerics;
using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

/// <summary>
/// Grouped numeric summer for any <see cref="INumber{T}"/> type.
/// </summary>
public class NumberGroupTimeSeriesSummer<TNumber>(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples) : BaseGroupNumberTimeSeriesSummer<TNumber, NumberTimeSeriesSummer<TNumber>, NumberGroupTimeSeriesSummer<TNumber>>(sampleInterval, deleteOverdueSamples)
    where TNumber : struct, INumber<TNumber>
{
    private readonly int _samplesCount = samplesCount;
    private readonly Strategy _strategy = strategy;

    /// <summary>
    /// Gets the maximum number of buckets to retain per key. Use 0 for unbounded.
    /// </summary>
    public int MaxSamplesCount => _samplesCount;

    /// <summary>
    /// Gets the aggregation strategy used for each key.
    /// </summary>
    public Strategy Strategy => _strategy;

    public NumberGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, bool deleteOverdueSamples)
        : this(sampleInterval, samplesCount, Strategy.Sum, deleteOverdueSamples)
    {
    }

    public NumberGroupTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
        : this(sampleInterval, 0, Strategy.Sum, deleteOverdueSamples)
    {
    }

    /// <inheritdoc />
    public override TNumber Average()
    {
        if (TimeSeries.IsEmpty)
        {
            return TNumber.Zero;
        }

        var total = TNumber.Zero;
        var sampleCount = 0;

        foreach (var summer in TimeSeries.Values)
        {
            total += summer.Sum();
            sampleCount += summer.Samples.Count;
        }

        return sampleCount == 0 ? TNumber.Zero : total / TNumber.CreateChecked(sampleCount);
    }

    /// <inheritdoc />
    public override TNumber Min()
    {
        var hasValue = false;
        var min = TNumber.Zero;

        foreach (var summer in TimeSeries.Values)
        {
            if (summer.Min() is not TNumber minValue)
            {
                continue;
            }

            if (!hasValue)
            {
                min = minValue;
                hasValue = true;
            }
            else
            {
                min = TNumber.Min(min, minValue);
            }
        }

        return hasValue ? min : TNumber.Zero;
    }

    /// <inheritdoc />
    public override TNumber Max()
    {
        var hasValue = false;
        var max = TNumber.Zero;

        foreach (var summer in TimeSeries.Values)
        {
            if (summer.Max() is not TNumber maxValue)
            {
                continue;
            }

            if (!hasValue)
            {
                max = maxValue;
                hasValue = true;
            }
            else
            {
                max = TNumber.Max(max, maxValue);
            }
        }

        return hasValue ? max : TNumber.Zero;
    }

    /// <inheritdoc />
    public override TNumber Sum()
    {
        var total = TNumber.Zero;
        foreach (var summer in TimeSeries.Values)
        {
            total += summer.Sum();
        }

        return total;
    }

    protected override NumberTimeSeriesSummer<TNumber> CreateSummer()
    {
        return new NumberTimeSeriesSummer<TNumber>(SampleInterval, _samplesCount, _strategy);
    }
}
