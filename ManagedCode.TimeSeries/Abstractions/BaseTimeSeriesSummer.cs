using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeriesSummer<TSummerItem, TSelf> : BaseTimeSeries<TSummerItem, TSummerItem, TSelf>
    where TSummerItem : ISummerItem<TSummerItem> where TSelf : BaseTimeSeries<TSummerItem, TSummerItem, TSelf>
{
    private readonly Strategy _strategy;

    protected BaseTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount)
    {
        _strategy = strategy;
    }

    protected override void AddData(DateTimeOffset date, TSummerItem data)
    {
        if (!Samples.ContainsKey(date))
        {
            Samples.Add(date, TSummerItem.Zero);
        }

        Samples[date] = Update(Samples[date], data);
    }

    public override void Merge(TSelf accumulator)
    {
        DataCount += accumulator.DataCount;
        foreach (var sample in accumulator.Samples.ToArray())
        {
            if (Samples.ContainsKey(sample.Key))
            {
                Samples[sample.Key] = Update(Samples[sample.Key], sample.Value);
            }
            else
            {
                Samples.Add(sample.Key, sample.Value);
            }
        }

        CheckSamplesSize();
    }

    public override void Resample(TimeSpan sampleInterval, int samplesCount)
    {
        if (sampleInterval <= SampleInterval)
        {
            throw new InvalidOperationException();
        }

        SampleInterval = sampleInterval;
        MaxSamplesCount = samplesCount;

        var samples = Samples;

        Samples = new Dictionary<DateTimeOffset, TSummerItem>();

        foreach (var (key, value) in samples)
        {
            AddNewData(key, value);
        }
    }

    private TSummerItem Update(TSummerItem left, TSummerItem right)
    {
        return _strategy switch
        {
            Strategy.Sum => left + right,
            Strategy.Min => TSummerItem.Min(left, right),
            Strategy.Max => TSummerItem.Max(left, right),
            Strategy.Replace => right,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Increment()
    {
        AddNewData(TSummerItem.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Decrement()
    {
        AddNewData(-TSummerItem.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem? Min()
    {
        lock (Samples)
        {
            return Samples.Values.Min();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem? Max()
    {
        lock (Samples)
        {
            return Samples.Values.Max();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem Sum()
    {
        lock (Samples)
        {
            return Samples.Aggregate(TSummerItem.Zero, (current, sample) => current + sample.Value);
        }
    }
}