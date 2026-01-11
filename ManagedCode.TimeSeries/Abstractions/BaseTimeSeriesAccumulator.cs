using System.Collections.Concurrent;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for accumulators which store raw events in per-bucket queues.
/// </summary>
public abstract class BaseTimeSeriesAccumulator<T, TSelf> : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf> where TSelf : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf>
{
    protected BaseTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount) : base(sampleInterval, maxSamplesCount)
    {
    }

    protected BaseTimeSeriesAccumulator(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end,
        DateTimeOffset lastDate)
        : base(sampleInterval, maxSamplesCount, start, end, lastDate)
    {
    }

    protected override void AddData(DateTimeOffset date, T data)
    {
        var queue = GetOrCreateSample(date, static () => new ConcurrentQueue<T>());
        queue.Enqueue(data);
    }

    /// <summary>
    /// Removes empty buckets from both ends of the series.
    /// </summary>
    public void Trim()
    {
        TrimStart();
        TrimEnd();
    }

    /// <summary>
    /// Removes empty buckets from the beginning of the series.
    /// </summary>
    public void TrimStart()
    {
        while (TryGetBoundarySample(static (candidate, current) => candidate < current, out var key, out var queue))
        {
            if (!queue.IsEmpty)
            {
                return;
            }

            if (Storage.TryRemove(key, out _))
            {
                RecalculateRange();
                continue;
            }

            // Removal failed due to contention, retry.
        }
    }

    /// <summary>
    /// Removes empty buckets from the end of the series.
    /// </summary>
    public void TrimEnd()
    {
        while (TryGetBoundarySample(static (candidate, current) => candidate > current, out var key, out var queue))
        {
            if (!queue.IsEmpty)
            {
                return;
            }

            if (Storage.TryRemove(key, out _))
            {
                RecalculateRange();
                continue;
            }

            // Contention, retry.
        }
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
            var queue = GetOrCreateSample(sample.Key, static () => new ConcurrentQueue<T>());
            if (ReferenceEquals(queue, sample.Value))
            {
                continue;
            }

            foreach (var item in sample.Value)
            {
                queue.Enqueue(item);
            }
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
            var queue = GetOrCreateSample(roundedKey, static () => new ConcurrentQueue<T>());

            foreach (var item in value)
            {
                queue.Enqueue(item);
            }

            if (!lastObservedSample.HasValue || roundedKey > lastObservedSample.Value)
            {
                lastObservedSample = roundedKey;
            }
        }

        SetDataCount(previousCount);
        LastDate = lastObservedSample ?? previousLastDate;
    }

    private bool TryGetBoundarySample(Func<DateTimeOffset, DateTimeOffset, bool> comparer, out DateTimeOffset key, out ConcurrentQueue<T> queue)
    {
        key = default;
        queue = default!;
        var found = false;

        foreach (var sample in Storage)
        {
            if (!found || comparer(sample.Key, key))
            {
                key = sample.Key;
                queue = sample.Value;
                found = true;
            }
        }

        return found;
    }
}
