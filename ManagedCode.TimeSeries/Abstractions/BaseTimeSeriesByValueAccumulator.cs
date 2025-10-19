namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeriesByValueAccumulator<T, TSelf> : BaseTimeSeries<T, HashSet<T>, TSelf>
    where TSelf : BaseTimeSeries<T, HashSet<T>, TSelf>
{
    protected BaseTimeSeriesByValueAccumulator(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount)
    {
    }

    protected BaseTimeSeriesByValueAccumulator(TimeSpan sampleInterval, int samplesCount, DateTimeOffset start, DateTimeOffset end,
        DateTimeOffset lastDate)
        : base(sampleInterval, samplesCount, start, end, lastDate)
    {
    }

    protected override void AddData(DateTimeOffset date, T data)
    {
        ref var set = ref GetOrCreateSample(date, out _);
        set ??= new HashSet<T>();
        set.Add(data);
    }

    // public BaseTimeSeriesByValueAccumulator<T> Trim()
    // {
    //     TrimStart();
    //     TrimEnd();
    //     return this;
    // }
    //
    // public BaseTimeSeriesByValueAccumulator<T> TrimStart()
    // {
    //     foreach (var item in Samples.ToArray())
    //     {
    //         if (item.Value.Count > 0)
    //         {
    //             break;
    //         }
    //
    //         Samples.Remove(item.Key);
    //     }
    //
    //     return this;
    // }
    //
    // public BaseTimeSeriesByValueAccumulator<T> TrimEnd()
    // {
    //     foreach (var item in Samples.Reverse().ToArray())
    //     {
    //         if (item.Value.Count > 0)
    //         {
    //             break;
    //         }
    //
    //         Samples.Remove(item.Key);
    //     }
    //
    //     return this;
    // }

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
                ref var set = ref GetOrCreateSample(sample.Key, out var created);
                if (created)
                {
                    set = sample.Value;
                }
                else
                {
                    set ??= new HashSet<T>();
                    set.UnionWith(sample.Value);
                }
            }

            CheckSamplesSize();
        }
    }
}
