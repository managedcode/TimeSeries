namespace ManagedCode.TimeSeries;

public abstract class BaseTimeSeriesAccumulator<T> : BaseTimeSeries<T, Queue<T>>
{
    protected BaseTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount)
    {
    }

    protected BaseTimeSeriesAccumulator(TimeSpan sampleInterval, int samplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
        : base(sampleInterval, samplesCount, start, end, lastDate)
    {
    }

    protected override void AddData(DateTimeOffset now, T data)
    {
        if (!Samples.ContainsKey(now))
        {
            Samples.Add(now, new Queue<T>());
        }

        Samples[now].Enqueue(data);
    }

    public BaseTimeSeriesAccumulator<T> Trim()
    {
        TrimStart();
        TrimEnd();
        return this;
    }

    public BaseTimeSeriesAccumulator<T> TrimStart()
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

    public BaseTimeSeriesAccumulator<T> TrimEnd()
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

    public BaseTimeSeriesAccumulator<T> Merge(BaseTimeSeriesAccumulator<T> accumulator)
    {
        DataCount += accumulator.DataCount;
        LastDate = accumulator.LastDate > LastDate ? accumulator.LastDate : LastDate;
        foreach (var sample in accumulator.Samples.ToArray())
        {
            if (Samples.TryGetValue(sample.Key, out var queue))
            {
                foreach (var q in sample.Value.ToArray())
                {
                    queue.Enqueue(q);
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