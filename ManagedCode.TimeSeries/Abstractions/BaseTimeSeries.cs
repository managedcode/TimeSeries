using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseTimeSeries<T, TSample, TSelf> :
    ITimeSeries<T, TSelf> where TSelf : BaseTimeSeries<T, TSample, TSelf>
{
    private const int DefaultSampleCount = 100;

    private readonly ConcurrentDictionary<DateTimeOffset, TSample> _samples = new();
    private DateTimeOffset _start;
    private DateTimeOffset _end;
    private DateTimeOffset _lastDate;
    private long _dataCount;
    private int _maxSamplesCount;

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount)
    {
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;

        var now = DateTimeOffset.UtcNow.Round(SampleInterval);
        Volatile.Write(ref _start, now);
        Volatile.Write(ref _end, now);
        Volatile.Write(ref _lastDate, now);
    }

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
    {
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;

        Volatile.Write(ref _start, start.Round(SampleInterval));
        Volatile.Write(ref _end, end.Round(SampleInterval));
        Volatile.Write(ref _lastDate, lastDate);

        foreach (var key in _samples.Keys)
        {
            _samples.TryRemove(key, out _);
        }
    }

    public ConcurrentDictionary<DateTimeOffset, TSample> Samples => _samples;

    public DateTimeOffset Start
    {
        get => Volatile.Read(ref _start);
        internal set => Volatile.Write(ref _start, value);
    }

    public DateTimeOffset End
    {
        get => Volatile.Read(ref _end);
        protected set => Volatile.Write(ref _end, value);
    }

    public DateTimeOffset LastDate
    {
        get => Volatile.Read(ref _lastDate);
        protected set => Volatile.Write(ref _lastDate, value);
    }

    public TimeSpan SampleInterval { get; protected set; }

    public int MaxSamplesCount
    {
        get => Volatile.Read(ref _maxSamplesCount);
        protected set => Volatile.Write(ref _maxSamplesCount, value);
    }

    public ulong DataCount => unchecked((ulong)Volatile.Read(ref _dataCount));

    internal void InitInternal(Dictionary<DateTimeOffset, TSample> samples,
        DateTimeOffset start, DateTimeOffset end,
        DateTimeOffset lastDate,
        ulong dataCount)
    {
        _samples.Clear();
        foreach (var kvp in samples)
        {
            _samples.TryAdd(kvp.Key, kvp.Value);
        }

        Volatile.Write(ref _start, start);
        Volatile.Write(ref _end, end);
        Volatile.Write(ref _lastDate, lastDate);
        Volatile.Write(ref _dataCount, unchecked((long)dataCount));
    }

    public bool IsFull => _samples.Count >= MaxSamplesCount;
    public bool IsEmpty => _samples.IsEmpty;
    public bool IsOverflow => MaxSamplesCount > 0 && _samples.Count > MaxSamplesCount;

    public abstract void Resample(TimeSpan sampleInterval, int samplesCount);

    public static TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0)
    {
        return (TSelf)Activator.CreateInstance(typeof(TSelf), sampleInterval ?? TimeSpan.Zero, maxSamplesCount)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract void Merge(TSelf accumulator);

    public void AddNewData(T data)
    {
        var now = DateTimeOffset.UtcNow;
        var rounded = now.Round(SampleInterval);

        Interlocked.Increment(ref _dataCount);
        AddData(rounded, data);

        UpdateEnd(rounded);
        Volatile.Write(ref _lastDate, now);
    }

    public void AddNewData(DateTimeOffset dateTimeOffset, T data)
    {
        var rounded = dateTimeOffset.Round(SampleInterval);

        Interlocked.Increment(ref _dataCount);
        AddData(rounded, data);

        UpdateEnd(rounded);
        Volatile.Write(ref _lastDate, rounded);
    }

    public void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past)
    {
        var samples = MaxSamplesCount > 0 ? MaxSamplesCount : DefaultSampleCount;

        if (direction is MarkupDirection.Past or MarkupDirection.Feature)
        {
            var cursor = Start;
            for (var i = 0; i < samples; i++)
            {
                cursor = cursor.Round(SampleInterval);
                _ = GetOrCreateSample(cursor, static () => Activator.CreateInstance<TSample>()!);
                cursor = direction is MarkupDirection.Feature ? cursor.Add(SampleInterval) : cursor.Subtract(SampleInterval);
            }
        }
        else
        {
            var forward = Start;
            var backward = Start;

            for (var i = 0; i < samples / 2 + 1; i++)
            {
                forward = forward.Round(SampleInterval);
                backward = backward.Round(SampleInterval);

                _ = GetOrCreateSample(forward, static () => Activator.CreateInstance<TSample>()!);
                _ = GetOrCreateSample(backward, static () => Activator.CreateInstance<TSample>()!);

                forward = forward.Add(SampleInterval);
                backward = backward.Subtract(SampleInterval);
            }
        }
    }

    public void DeleteOverdueSamples()
    {
        if (MaxSamplesCount <= 0 || _samples.IsEmpty)
        {
            return;
        }

        var threshold = DateTimeOffset.UtcNow.Round(SampleInterval);
        for (var i = 0; i < MaxSamplesCount; i++)
        {
            threshold = threshold.Subtract(SampleInterval);
        }

        foreach (var key in _samples.Keys)
        {
            if (key < threshold)
            {
                _samples.TryRemove(key, out _);
            }
        }

        RecalculateRange();
    }

    public TSelf Rebase(IEnumerable<TSelf> accumulators)
    {
        var empty = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;

        foreach (var accumulator in accumulators)
        {
            Merge(accumulator);
        }

        return empty;
    }

    public void Merge(IEnumerable<TSelf> accumulators)
    {
        foreach (var accumulator in accumulators)
        {
            Merge(accumulator);
        }
    }

    public TSelf Rebase(TSelf accumulator)
    {
        var empty = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;
        empty.Merge(accumulator);

        return empty;
    }

    public static TSelf operator +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        return left.Rebase(right);
    }

    public static TSelf operator checked +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        return left.Rebase(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void AddData(DateTimeOffset now, T data);

    protected void AddToDataCount(ulong value)
    {
        if (value == 0)
        {
            return;
        }

        Interlocked.Add(ref _dataCount, unchecked((long)value));
    }

    protected TSample GetOrCreateSample(DateTimeOffset key, Func<TSample> factory)
    {
        while (true)
        {
            if (_samples.TryGetValue(key, out var existing))
            {
                UpdateRange(key);
                return existing;
            }

            var created = factory();
            if (_samples.TryAdd(key, created))
            {
                UpdateOnAdd(key);
                return created;
            }
        }
    }

    protected TSample AddOrUpdateSample(DateTimeOffset key, Func<TSample> addFactory, Func<TSample, TSample> updateFactory)
    {
        while (true)
        {
            if (_samples.TryGetValue(key, out var existing))
            {
                var updated = updateFactory(existing);
                if (_samples.TryUpdate(key, updated, existing))
                {
                    UpdateRange(key);
                    return updated;
                }

                continue;
            }

            var created = addFactory();
            if (_samples.TryAdd(key, created))
            {
                UpdateOnAdd(key);
                return created;
            }
        }
    }

    protected void ResetSamplesStorage()
    {
        _samples.Clear();
        var now = DateTimeOffset.UtcNow.Round(SampleInterval);
        Volatile.Write(ref _start, now);
        Volatile.Write(ref _end, now);
    }

    private void UpdateOnAdd(DateTimeOffset key)
    {
        UpdateRange(key);
        EnsureCapacity();
    }

    private void UpdateEnd(DateTimeOffset key)
    {
        UpdateRange(key);
    }

    private void UpdateRange(DateTimeOffset key)
    {
        var start = Start;
        if (start == default || key < start)
        {
            Volatile.Write(ref _start, key);
        }

        var end = End;
        if (end == default || key > end)
        {
            Volatile.Write(ref _end, key);
        }
    }

    private void EnsureCapacity()
    {
        if (MaxSamplesCount <= 0)
        {
            return;
        }

        while (_samples.Count > MaxSamplesCount)
        {
            if (!TryRemoveOldest())
            {
                break;
            }
        }
    }

    private bool TryRemoveOldest()
    {
        DateTimeOffset oldest = DateTimeOffset.MaxValue;
        foreach (var key in _samples.Keys)
        {
            if (key < oldest)
            {
                oldest = key;
            }
        }

        if (oldest == DateTimeOffset.MaxValue)
        {
            return false;
        }

        var removed = _samples.TryRemove(oldest, out _);
        if (removed)
        {
            RecalculateRange();
        }

        return removed;
    }

    protected void RecalculateRange()
    {
        DateTimeOffset min = DateTimeOffset.MaxValue;
        DateTimeOffset max = DateTimeOffset.MinValue;

        foreach (var key in _samples.Keys)
        {
            if (key < min)
            {
                min = key;
            }

            if (key > max)
            {
                max = key;
            }
        }

        if (min == DateTimeOffset.MaxValue)
        {
            var now = DateTimeOffset.UtcNow.Round(SampleInterval);
            Volatile.Write(ref _start, now);
            Volatile.Write(ref _end, now);
            return;
        }

        Volatile.Write(ref _start, min);
        Volatile.Write(ref _end, max);
    }

}
