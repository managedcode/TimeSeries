using System.Collections.Concurrent;
using System.Linq;

namespace ManagedCode.TimeSeries.Abstractions;

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

    public void Trim()
    {
        TrimStart();
        TrimEnd();
    }

    public void TrimStart()
    {
        while (TryGetBoundarySample(static (candidate, current) => candidate < current, out var key, out var queue))
        {
            if (!queue.IsEmpty)
            {
                return;
            }

            if (Samples.TryRemove(key, out _))
            {
                RecalculateRange();
                continue;
            }

            // Removal failed due to contention, retry.
        }
    }

    public void TrimEnd()
    {
        while (TryGetBoundarySample(static (candidate, current) => candidate > current, out var key, out var queue))
        {
            if (!queue.IsEmpty)
            {
                return;
            }

            if (Samples.TryRemove(key, out _))
            {
                RecalculateRange();
                continue;
            }

            // Contention, retry.
        }
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

    public override void Resample(TimeSpan sampleInterval, int samplesCount)
    {
        if (sampleInterval <= SampleInterval)
        {
            throw new InvalidOperationException();
        }

        SampleInterval = sampleInterval;
        MaxSamplesCount = samplesCount;

        var snapshot = Samples.ToArray();
        ResetSamplesStorage();

        foreach (var (key, value) in snapshot)
        {
            foreach (var item in value)
            {
                AddNewData(key, item);
            }
        }
    }

    private bool TryGetBoundarySample(Func<DateTimeOffset, DateTimeOffset, bool> comparer, out DateTimeOffset key, out ConcurrentQueue<T> queue)
    {
        key = default;
        queue = default!;
        var found = false;

        foreach (var sample in Samples)
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
