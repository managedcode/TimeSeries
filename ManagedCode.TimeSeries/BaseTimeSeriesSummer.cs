using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries;

public abstract class BaseTimeSeriesSummer<T> : BaseTimeSeries<T, T>
{
    protected BaseTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount)
    {
    }

    protected override void AddData(DateTimeOffset now, T data)
    {
        if (!Samples.ContainsKey(now))
        {
            Samples.Add(now, default);
        }

        Samples[now] = Plus(Samples[now], data);
    }

    public BaseTimeSeriesSummer<T> Merge(BaseTimeSeriesSummer<T> accumulator)
    {
        DataCount += accumulator.DataCount;
        foreach (var sample in accumulator.Samples.ToArray())
        {
            if (Samples.ContainsKey(sample.Key))
            {
                Samples[sample.Key] = Plus(Samples[sample.Key], sample.Value);
            }
            else
            {
                Samples.Add(sample.Key, sample.Value);
            }
        }

        CheckSamplesSize();

        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract T Plus(T left, T right);
}