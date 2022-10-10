using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries;

public enum Strategy
{
    Sum,
    Min,
    Max
}

public abstract class BaseTimeSeriesSummer<T> : BaseTimeSeries<T, T> 
{
    private readonly Strategy _strategy;

    protected BaseTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount)
    {
        _strategy = strategy;
    }

    protected override void AddData(DateTimeOffset now, T data)
    {
        if (!Samples.ContainsKey(now))
        {
            Samples.Add(now, default);
        }

        Samples[now] = Update(Samples[now], data);
    }

    public BaseTimeSeriesSummer<T> Merge(BaseTimeSeriesSummer<T> accumulator)
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

    private T Update(T left, T right)
    {
        return _strategy switch
        {
            Strategy.Sum => Plus(left, right),
            Strategy.Min => Min(left, right),
            Strategy.Max => Max(left, right),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract T Plus(T left, T right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract T Min(T left, T right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract T Max(T left, T right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract T Average();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract void Increment();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract void Decrement();
}