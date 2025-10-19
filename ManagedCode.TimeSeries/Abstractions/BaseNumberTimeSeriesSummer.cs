using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseNumberTimeSeriesSummer<TNumber, TSelf> : BaseTimeSeries<TNumber, TNumber, TSelf>
    where TNumber : struct, INumber<TNumber> where TSelf : BaseTimeSeries<TNumber, TNumber, TSelf>
{
    protected BaseNumberTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount)
    {
        Strategy = strategy;
    }
    
    public Strategy Strategy { get; protected set; }

    protected override void AddData(DateTimeOffset date, TNumber data)
    {
        AddOrUpdateSample(date,
            () => data,
            current => Update(current, data));
    }

    public override void Merge(TSelf accumulator)
    {
        if (accumulator is null)
        {
            return;
        }

        AddToDataCount(accumulator.DataCount);
        if (accumulator.LastDate > LastDate)
        {
            LastDate = accumulator.LastDate;
        }

        foreach (var sample in accumulator.Samples)
        {
            AddOrUpdateSample(sample.Key,
                () => sample.Value,
                current => Update(current, sample.Value));
        }
    }

    public override void Resample(TimeSpan sampleInterval, int samplesCount)
    {
        if (sampleInterval <= SampleInterval)
        {
            throw new InvalidOperationException();
        }

        SampleInterval = sampleInterval;
        MaxSamplesCount = samplesCount;

        var snapshot = Storage.ToArray();
        ResetSamplesStorage();

        foreach (var (key, value) in snapshot)
        {
            AddNewData(key, value);
        }
    }

    private TNumber Update(TNumber left, TNumber right)
    {
        return Strategy switch
        {
            Strategy.Sum => left + right,
            Strategy.Min => TNumber.Min(left, right),
            Strategy.Max => TNumber.Max(left, right),
            Strategy.Replace => right,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Increment()
    {
        AddNewData(TNumber.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Decrement()
    {
        AddNewData(-TNumber.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Average()
    {
        var total = TNumber.Zero;
        var count = 0;

        foreach (var value in Samples.Values)
        {
            total += value;
            count++;
        }

        return count == 0 ? TNumber.Zero : total / TNumber.CreateChecked(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Min()
    {
        var hasValue = false;
        var min = TNumber.Zero;

        foreach (var value in Samples.Values)
        {
            if (!hasValue)
            {
                min = value;
                hasValue = true;
            }
            else
            {
                min = TNumber.Min(min, value);
            }
        }

        return hasValue ? min : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Max()
    {
        var hasValue = false;
        var max = TNumber.Zero;

        foreach (var value in Samples.Values)
        {
            if (!hasValue)
            {
                max = value;
                hasValue = true;
            }
            else
            {
                max = TNumber.Max(max, value);
            }
        }

        return hasValue ? max : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Sum()
    {
        var total = TNumber.Zero;
        foreach (var sample in Samples.Values)
        {
            total += sample;
        }

        return total;
    }
}
