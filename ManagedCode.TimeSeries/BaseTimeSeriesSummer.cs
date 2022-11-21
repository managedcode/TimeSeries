using System.Numerics;
using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries;

public enum Strategy
{
    Sum,
    Min,
    Max,
    Replace,
}

public abstract class BaseTimeSeriesSummer<TNumber> : BaseTimeSeries<TNumber, TNumber> where TNumber : INumber<TNumber>
{
    private readonly Strategy _strategy;

    protected BaseTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount)
    {
        _strategy = strategy;
    }

    protected override void AddData(DateTimeOffset now, TNumber data)
    {
        if (!Samples.ContainsKey(now))
        {
            Samples.Add(now, TNumber.Zero);
        }

        Samples[now] = Update(Samples[now], data);
    }

    public BaseTimeSeriesSummer<TNumber> Merge(BaseTimeSeriesSummer<TNumber> accumulator)
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

        return this;
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