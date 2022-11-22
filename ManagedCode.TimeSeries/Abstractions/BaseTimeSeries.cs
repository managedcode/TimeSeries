using System.Runtime.CompilerServices;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeries<T, TSample, TSelf> : ITimeSeries<T, TSelf> where TSelf : BaseTimeSeries<T, TSample, TSelf>
{
    private const int DefaultSampleCount = 100;
    protected readonly object _sync = new();

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount)
    {
        MaxSamplesCount = maxSamplesCount;
        SampleInterval = sampleInterval;
        Start = DateTimeOffset.UtcNow.Round(SampleInterval);
        End = Start;
    }

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
    {
        MaxSamplesCount = maxSamplesCount;
        SampleInterval = sampleInterval;
        Start = start.Round(SampleInterval);
        End = end.Round(SampleInterval);
        LastDate = lastDate;
    }

    public Dictionary<DateTimeOffset, TSample> Samples { get; protected set; } = new();
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; protected set; }

    public TimeSpan SampleInterval { get; protected set; }
    public int MaxSamplesCount { get; protected set; }

    public DateTimeOffset LastDate { get; protected set; }

    public ulong DataCount { get; protected set; }

    public bool IsFull => Samples.Count >= MaxSamplesCount;
    public bool IsEmpty => Samples.Count == 0;
    public bool IsOverflow => Samples.Count > MaxSamplesCount;

    public abstract void Resample(TimeSpan sampleInterval, int samplesCount);

    public static TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0)
    {
        return (TSelf) Activator.CreateInstance(typeof(TSelf), sampleInterval ?? TimeSpan.Zero, maxSamplesCount)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract void Merge(TSelf accumulator);

    public void AddNewData(T data)
    {
        var rounded = DateTimeOffset.UtcNow.Round(SampleInterval);
        lock (_sync)
        {
            DataCount += 1;
            AddData(rounded, data);
            End = rounded;
            LastDate = DateTimeOffset.UtcNow;
            CheckSamplesSize();
        }
    }

    public void AddNewData(DateTimeOffset dateTimeOffset, T data)
    {
        lock (_sync)
        {
            DataCount += 1;
            AddData(dateTimeOffset.Round(SampleInterval), data);
            CheckSamplesSize();
        }
    }

    public void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past)
    {
        var samples = MaxSamplesCount > 0 ? MaxSamplesCount : DefaultSampleCount;

        if (direction is MarkupDirection.Past or MarkupDirection.Feature)
        {
            var now = Start;
            for (var i = 0; i < samples; i++)
            {
                now = now.Round(SampleInterval);
                if (!Samples.ContainsKey(now))
                {
                    Samples.Add(now, Activator.CreateInstance<TSample>());
                }

                now = direction is MarkupDirection.Feature ? now.Add(SampleInterval) : now.Subtract(SampleInterval);
            }
        }
        else
        {
            var nowForFeature = Start;
            var nowForPast = Start;

            for (var i = 0; i < samples / 2 + 1; i++)
            {
                nowForFeature = nowForFeature.Round(SampleInterval);
                nowForPast = nowForPast.Round(SampleInterval);

                if (!Samples.ContainsKey(nowForFeature))
                {
                    Samples.Add(nowForFeature, Activator.CreateInstance<TSample>());
                }

                if (!Samples.ContainsKey(nowForPast))
                {
                    Samples.Add(nowForPast, Activator.CreateInstance<TSample>());
                }

                nowForFeature = nowForFeature.Add(SampleInterval);
                nowForPast = nowForPast.Subtract(SampleInterval);
            }
        }
    }

    public void DeleteOverdueSamples()
    {
        var dateTime = DateTimeOffset.UtcNow.Round(SampleInterval);
        for (int i = 0; i < MaxSamplesCount; i++)
        {
            dateTime = dateTime.Subtract(SampleInterval);
        }

        lock (_sync)
        {
            foreach (var date in Samples.Keys.ToArray())
            {
                if (date < dateTime)
                {
                    Samples.Remove(date);
                }
            }
        }
    }

    public TSelf Rebase(IEnumerable<TSelf> accumulators)
    {
        var empty = (TSelf) Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;

        foreach (var accumulator in accumulators)
        {
            Merge(accumulator);
        }

        return empty;
    }

    public void Merge(IEnumerable<TSelf> accumulators)
    {
        foreach (var accumulator in accumulators)
        {
            Merge(accumulator);
        }
    }

    public TSelf Rebase(TSelf accumulator)
    {
        var empty = (TSelf) Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;
        empty.Merge(accumulator);

        return empty;
    }

    public static TSelf operator +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        return left.Rebase(right);
    }

    public static TSelf operator checked +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        return left.Rebase(right);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void AddData(DateTimeOffset now, T data);

    protected void CheckSamplesSize()
    {
        lock (_sync)
        {
            if (MaxSamplesCount <= 0)
            {
                return;
            }

            while (IsOverflow)
            {
                Samples.Remove(Samples.Keys.MinBy(o => o)); //check performance here
            }
        }
    }
}