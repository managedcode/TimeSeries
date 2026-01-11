using System.Runtime.CompilerServices;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for summers which aggregate values per bucket.
/// </summary>
public abstract class BaseTimeSeriesSummer<TSummerItem, TSelf>(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : BaseTimeSeries<TSummerItem, TSummerItem, TSelf>(sampleInterval, maxSamplesCount)
    where TSummerItem : ISummerItem<TSummerItem> where TSelf : BaseTimeSeries<TSummerItem, TSummerItem, TSelf>
{
    private readonly Strategy _strategy = strategy;

    protected override void AddData(DateTimeOffset date, TSummerItem data)
    {
        AddOrUpdateSample(date,
            () => data,
            current => Update(current, data));
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
            AddOrUpdateSample(sample.Key,
                () => sample.Value,
                current => Update(current, sample.Value));
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
            AddOrUpdateSample(roundedKey,
                () => value,
                current => Update(current, value));

            if (!lastObservedSample.HasValue || roundedKey > lastObservedSample.Value)
            {
                lastObservedSample = roundedKey;
            }
        }

        SetDataCount(previousCount);
        LastDate = lastObservedSample ?? previousLastDate;
    }

    private TSummerItem Update(TSummerItem left, TSummerItem right)
    {
        return _strategy switch
        {
            Strategy.Sum => left + right,
            Strategy.Min => TSummerItem.Min(left, right),
            Strategy.Max => TSummerItem.Max(left, right),
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
        AddNewData(TSummerItem.One);
    }

    /// <summary>
    /// Decrements the current bucket by one.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Decrement()
    {
        AddNewData(-TSummerItem.One);
    }

    /// <summary>
    /// Returns the minimum value across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem? Min()
    {
        var hasValue = false;
        var min = TSummerItem.Zero;

        foreach (var value in Storage.Values)
        {
            if (!hasValue)
            {
                min = value;
                hasValue = true;
            }
            else
            {
                min = TSummerItem.Min(min, value);
            }
        }

        return hasValue ? min : default;
    }

    /// <summary>
    /// Returns the maximum value across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem? Max()
    {
        var hasValue = false;
        var max = TSummerItem.Zero;

        foreach (var value in Storage.Values)
        {
            if (!hasValue)
            {
                max = value;
                hasValue = true;
            }
            else
            {
                max = TSummerItem.Max(max, value);
            }
        }

        return hasValue ? max : default;
    }

    /// <summary>
    /// Returns the sum across all buckets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem Sum()
    {
        var total = TSummerItem.Zero;
        foreach (var value in Storage.Values)
        {
            total += value;
        }

        return total;
    }
}
