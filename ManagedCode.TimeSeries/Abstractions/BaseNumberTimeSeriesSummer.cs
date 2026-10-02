using System.Numerics;
using System.Runtime.CompilerServices;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for numeric summers which aggregate numeric values per bucket.
/// </summary>
public abstract class BaseNumberTimeSeriesSummer<TNumber, TSelf>(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : BaseTimeSeries<TNumber, TNumber, TSelf>(sampleInterval, maxSamplesCount)
    where TNumber : struct, INumber<TNumber> where TSelf : BaseTimeSeries<TNumber, TNumber, TSelf>
{
    /// <summary>
    /// Gets the aggregation strategy for this summer.
    /// </summary>
    public Strategy Strategy { get; protected set; } = strategy;

    protected override void AddData(DateTimeOffset date, TNumber data)
    {
        var state = (Owner: this, Value: data);
        AddOrUpdateSample(
            date,
            state,
            static context => context.Value,
            static (current, context) => context.Owner.Update(current, context.Value));
    }

    /// <inheritdoc />
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
            var state = (Owner: this, Value: sample.Value);
            AddOrUpdateSample(
                sample.Key,
                state,
                static context => context.Value,
                static (current, context) => context.Owner.Update(current, context.Value));
        }
    }

    /// <inheritdoc />
    public override void Resample(TimeSpan sampleInterval, int samplesCount)
    {
        if (sampleInterval <= SampleInterval)
        {
            throw new InvalidOperationException();
        }

        SampleInterval = sampleInterval;
        MaxSamplesCount = samplesCount;

        var previousCount = DataCount;
        var previousLastDate = LastDate;

        var snapshot = Storage.ToArray();
        ResetSamplesStorage();
        SetDataCount(0);

        DateTimeOffset? lastObservedSample = null;

        foreach (var (key, value) in snapshot)
        {
            var roundedKey = key.RoundUtc(SampleInterval);
            var state = (Owner: this, Value: value);
            AddOrUpdateSample(
                roundedKey,
                state,
                static context => context.Value,
                static (current, context) => context.Owner.Update(current, context.Value));

            if (!lastObservedSample.HasValue || roundedKey > lastObservedSample.Value)
            {
                lastObservedSample = roundedKey;
            }
        }

        SetDataCount(previousCount);
        LastDate = lastObservedSample ?? previousLastDate;
    }

    private TNumber Update(TNumber left, TNumber right)
    {
        return Strategy switch
        {
            Strategy.Sum => left + right,
            Strategy.Min => TNumber.Min(left, right),
            Strategy.Max => TNumber.Max(left, right),
            Strategy.Replace => right,
            _ => throw new InvalidOperationException("Unsupported strategy.")
        };
    }

    /// <summary>
    /// Increments the current bucket by one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Increment()
    {
        AddNewData(TNumber.One);
    }

    /// <summary>
    /// Decrements the current bucket by one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Decrement()
    {
        AddNewData(-TNumber.One);
    }

    /// <summary>
    /// Returns the average across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Average()
    {
        var total = TNumber.Zero;
        var count = 0;

        foreach (var value in Storage.Values)
        {
            total += value;
            count++;
        }

        return count == 0 ? TNumber.Zero : total / TNumber.CreateChecked(count);
    }

    /// <summary>
    /// Returns the minimum value across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Min()
    {
        var hasValue = false;
        var min = TNumber.Zero;

        foreach (var value in Storage.Values)
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

    /// <summary>
    /// Returns the maximum value across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Max()
    {
        var hasValue = false;
        var max = TNumber.Zero;

        foreach (var value in Storage.Values)
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

    /// <summary>
    /// Returns the sum across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Sum()
    {
        var total = TNumber.Zero;
        foreach (var sample in Storage.Values)
        {
            total += sample;
        }

        return total;
    }
}
