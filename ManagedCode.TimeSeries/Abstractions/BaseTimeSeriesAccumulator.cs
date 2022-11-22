using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeriesAccumulator<T, TSelf> : BaseTimeSeries<T, Queue<T>, TSelf> where TSelf : ITimeSeries<T, Queue<T>, TSelf>
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
        if (!Samples.ContainsKey(date))
        {
            Samples.Add(date, new Queue<T>());
        }

        Samples[date].Enqueue(data);
    }

    public void Trim()
    {
        TrimStart();
        TrimEnd();
    }

    public void TrimStart()
    {
        foreach (var item in Samples.ToArray())
        {
            if (item.Value.Count > 0)
            {
                break;
            }

            Samples.Remove(item.Key);
        }
    }

    public void TrimEnd()
    {
        foreach (var item in Samples.Reverse().ToArray())
        {
            if (item.Value.Count > 0)
            {
                break;
            }

            Samples.Remove(item.Key);
        }
    }

    public override void Merge(TSelf accumulator)
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

        Samples = new Dictionary<DateTimeOffset, Queue<T>>();

        foreach (var (key, value) in samples)
        {
            foreach (var v in value)
            {
                AddNewData(key, v);
            }
        }
    }
}