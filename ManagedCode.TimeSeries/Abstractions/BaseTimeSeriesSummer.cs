using System.Numerics;
using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries.Abstractions;

public enum Strategy
{
    Sum,
    Min,
    Max,
    Replace,
}

public abstract class BaseTimeSeriesSummer<TNumber, TSelf> : BaseTimeSeries<TNumber, TNumber, TSelf>
    where TNumber : INumber<TNumber> where TSelf : BaseTimeSeries<TNumber, TNumber, TSelf>
{
    private readonly Strategy _strategy;

    protected BaseTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount)
    {
        _strategy = strategy;
    }

    protected override void AddData(DateTimeOffset date, TNumber data)
    {
        if (!Samples.ContainsKey(date))
        {
            Samples.Add(date, TNumber.Zero);
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

        Samples = new Dictionary<DateTimeOffset, TNumber>();

        foreach (var (key, value) in samples)
        {
            AddNewData(key, value);
        }
    }

    private TNumber Update(TNumber left, TNumber right)
    {
        return _strategy switch
        {
            Strategy.Sum => left + right,
            Strategy.Min => TNumber.Min(left, right),
            Strategy.Max => TNumber.Max(left, right),
            Strategy.Replace => right,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Increment()
    {
        AddNewData(TNumber.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Decrement()
    {
        AddNewData(-TNumber.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Average()
    {
        var sum = Sum();

        return TNumber.CreateChecked(sum) / TNumber.CreateChecked(Samples.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Min()
    {
        lock (Samples)
        {
            return Samples.Values.Min();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Max()
    {
        lock (Samples)
        {
            return Samples.Values.Max();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Sum()
    {
        lock (Samples)
        {
            return Samples.Aggregate(TNumber.Zero, (current, sample) => current + sample.Value);
        }
    }
}