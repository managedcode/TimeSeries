using System.Numerics;
using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class NumberGroupTimeSeriesSummer<TNumber> : BaseGroupNumberTimeSeriesSummer<TNumber, NumberTimeSeriesSummer<TNumber>, NumberGroupTimeSeriesSummer<TNumber>>
    where TNumber : struct, INumber<TNumber>
{
    private readonly TimeSpan _sampleInterval;
    private readonly int _samplesCount;
    private readonly Strategy _strategy;

    public NumberGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy, bool deleteOverdueSamples)
        : base(sampleInterval, deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _samplesCount = samplesCount;
        _strategy = strategy;
    }

    public NumberGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, bool deleteOverdueSamples)
        : this(sampleInterval, samplesCount, Strategy.Sum, deleteOverdueSamples)
    {
    }

    public NumberGroupTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
        : this(sampleInterval, 0, Strategy.Sum, deleteOverdueSamples)
    {
    }

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
        return new NumberTimeSeriesSummer<TNumber>(_sampleInterval, _samplesCount, _strategy);
    }
}
