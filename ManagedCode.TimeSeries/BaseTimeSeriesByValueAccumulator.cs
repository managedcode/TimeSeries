namespace ManagedCode.TimeSeries;

public abstract class BaseTimeSeriesByValueAccumulator<T> : BaseTimeSeries<T, HashSet<T>>
{
    protected BaseTimeSeriesByValueAccumulator(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount)
    {
    }

    protected BaseTimeSeriesByValueAccumulator(TimeSpan sampleInterval, int samplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
        : base(sampleInterval, samplesCount, start, end, lastDate)
    {
    }

    protected override void AddData(DateTimeOffset now, T data)
    {
        if (!Samples.ContainsKey(now))
        {
            Samples.Add(now, new HashSet<T>());
        }

        Samples[now].Add(data);
    }

    public BaseTimeSeriesByValueAccumulator<T> Trim()
    {
        TrimStart();
        TrimEnd();
        return this;
    }

    public BaseTimeSeriesByValueAccumulator<T> TrimStart()
    {
        foreach (var item in Samples.ToArray())
        {
            if (item.Value.Count > 0)
            {
                break;
            }

            Samples.Remove(item.Key);
        }

        return this;
    }

    public BaseTimeSeriesByValueAccumulator<T> TrimEnd()
    {
        foreach (var item in Samples.Reverse().ToArray())
        {
            if (item.Value.Count > 0)
            {
                break;
            }

            Samples.Remove(item.Key);
        }

        return this;
    }

    public BaseTimeSeriesByValueAccumulator<T> Merge(BaseTimeSeriesByValueAccumulator<T> accumulator)
    {
        DataCount += accumulator.DataCount;
        LastDate = accumulator.LastDate > LastDate ? accumulator.LastDate : LastDate;
        foreach (var sample in accumulator.Samples.ToArray())
        {
            if (Samples.TryGetValue(sample.Key, out var hashSet))
            {
                foreach (var v in sample.Value.ToArray())
                {
                    hashSet.Add(v);
                }
            }
            else
            {
                Samples.Add(sample.Key, sample.Value);
            }
        }

        CheckSamplesSize();

        return this;
    }
}