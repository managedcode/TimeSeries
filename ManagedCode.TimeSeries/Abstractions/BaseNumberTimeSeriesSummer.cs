using System.Numerics;
using System.Runtime.CompilerServices;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseNumberTimeSeriesSummer<TNumber, TSelf> : BaseTimeSeries<TNumber, TNumber, TSelf>
    where TNumber : struct, INumber<TNumber> where TSelf : BaseTimeSeries<TNumber, TNumber, TSelf>
{
    protected BaseNumberTimeSeriesSummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy) : base(sampleInterval, maxSamplesCount)
    {
        Strategy = strategy;
    }
    
    public Strategy Strategy { get; protected set; }

    protected override void AddData(DateTimeOffset date, TNumber data)
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

    private TNumber Update(TNumber left, TNumber right)
    {
        return Strategy switch
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
        lock (SyncRoot)
        {
            if (Samples.Count == 0)
            {
                return TNumber.Zero;
            }

            var total = TNumber.Zero;
            foreach (var value in Samples.Values)
            {
                total += value;
            }

            return total / TNumber.CreateChecked(Samples.Count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Min()
    {
        lock (SyncRoot)
        {
            if (Samples.Count == 0)
            {
                return null;
            }

            var enumerator = Samples.Values.GetEnumerator();
            enumerator.MoveNext();
            var min = enumerator.Current;

            while (enumerator.MoveNext())
            {
                min = TNumber.Min(min, enumerator.Current);
            }

            return min;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber? Max()
    {
        lock (SyncRoot)
        {
            if (Samples.Count == 0)
            {
                return null;
            }

            var enumerator = Samples.Values.GetEnumerator();
            enumerator.MoveNext();
            var max = enumerator.Current;

            while (enumerator.MoveNext())
            {
                max = TNumber.Max(max, enumerator.Current);
            }

            return max;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TNumber Sum()
    {
        lock (SyncRoot)
        {
            var total = TNumber.Zero;
            foreach (var sample in Samples.Values)
            {
                total += sample;
            }

            return total;
        }
    }
}
