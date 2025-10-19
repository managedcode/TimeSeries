using System.Collections.Concurrent;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeriesByValueAccumulator<T, TSelf> : BaseTimeSeries<T, ConcurrentDictionary<T, byte>, TSelf>
    where T : notnull
    where TSelf : BaseTimeSeries<T, ConcurrentDictionary<T, byte>, TSelf>
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
        var set = GetOrCreateSample(date, static () => new ConcurrentDictionary<T, byte>());
        set.TryAdd(data, 0);
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

        AddToDataCount(accumulator.DataCount);
        if (accumulator.LastDate > LastDate)
        {
            LastDate = accumulator.LastDate;
        }

        foreach (var sample in accumulator.Samples)
        {
            var set = GetOrCreateSample(sample.Key, static () => new ConcurrentDictionary<T, byte>());
            foreach (var key in sample.Value.Keys)
            {
                set.TryAdd(key, 0);
            }
        }
    }
}
