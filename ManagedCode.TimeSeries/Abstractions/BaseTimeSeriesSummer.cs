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
        ref var sample = ref GetOrCreateSample(date, out _);
        sample = Update(sample, data);
    }

    public override void Merge(TSelf accumulator)
    {
        if (accumulator is null)
        {
            return;
        }

        lock (SyncRoot)
        {
            DataCount += accumulator.DataCount;
            if (accumulator.LastDate > LastDate)
            {
                LastDate = accumulator.LastDate;
            }

            foreach (var sample in accumulator.Samples)
            {
                ref var target = ref GetOrCreateSample(sample.Key, out var created);
                target = created ? sample.Value : Update(target, sample.Value);
            }

            CheckSamplesSize();
        }
    }

    public override void Resample(TimeSpan sampleInterval, int samplesCount)
    {
        if (sampleInterval <= SampleInterval)
        {
            throw new InvalidOperationException();
        }

        lock (SyncRoot)
        {
            SampleInterval = sampleInterval;
            MaxSamplesCount = samplesCount;

            var snapshot = Samples;
            ResetSamplesStorage();

            foreach (var (key, value) in snapshot)
            {
                AddNewData(key, value);
            }
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
        lock (SyncRoot)
        {
            if (Samples.Count == 0)
            {
                return default;
            }

            var enumerator = Samples.Values.GetEnumerator();
            enumerator.MoveNext();
            var min = enumerator.Current;

            while (enumerator.MoveNext())
            {
                min = TSummerItem.Min(min, enumerator.Current);
            }

            return min;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem? Max()
    {
        lock (SyncRoot)
        {
            if (Samples.Count == 0)
            {
                return default;
            }

            var enumerator = Samples.Values.GetEnumerator();
            enumerator.MoveNext();
            var max = enumerator.Current;

            while (enumerator.MoveNext())
            {
                max = TSummerItem.Max(max, enumerator.Current);
            }

            return max;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TSummerItem Sum()
    {
        lock (SyncRoot)
        {
            var total = TSummerItem.Zero;
            foreach (var value in Samples.Values)
            {
                total += value;
            }

            return total;
        }
    }
}
