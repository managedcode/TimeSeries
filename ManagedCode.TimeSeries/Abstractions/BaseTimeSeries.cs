using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
    private AtomicDateTimeOffset _start;
    private AtomicDateTimeOffset _end;
    private AtomicDateTimeOffset _lastDate;
    private long _dataCount;
    private int _maxSamplesCount;

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount)
    {
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;

        var now = DateTimeOffset.UtcNow.Round(SampleInterval);
        _start.Write(now);
        _end.Write(now);
        _lastDate.Write(now);
    }

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
    {
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;

        _start.Write(start.Round(SampleInterval));
        _end.Write(end.Round(SampleInterval));
        _lastDate.Write(lastDate);

        foreach (var key in _samples.Keys)
        {
            _samples.TryRemove(key, out _);
        }
    }

    protected ConcurrentDictionary<DateTimeOffset, TSample> Storage => _samples;

    public IReadOnlyDictionary<DateTimeOffset, TSample> Samples => new OrderedSampleView(_samples);

    public DateTimeOffset Start
    {
        get => _start.Read();
        internal set => _start.Write(value);
    }

    public DateTimeOffset End
    {
        get => _end.Read();
        protected set => _end.Write(value);
    }

    public DateTimeOffset LastDate
    {
        get => _lastDate.Read();
        protected set => _lastDate.Write(value);
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

        _start.Write(start);
        _end.Write(end);
        _lastDate.Write(lastDate);
        Volatile.Write(ref _dataCount, unchecked((long)dataCount));
    }

    public bool IsFull => _samples.Count >= MaxSamplesCount;
    public bool IsEmpty => _samples.IsEmpty;
    public bool IsOverflow => MaxSamplesCount > 0 && _samples.Count > MaxSamplesCount;

    public abstract void Resample(TimeSpan sampleInterval, int samplesCount);

    public static TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0)
    {
        if (sampleInterval is null || sampleInterval.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be a positive value.");
        }

        return (TSelf)Activator.CreateInstance(typeof(TSelf), sampleInterval.Value, maxSamplesCount)!;
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
        _lastDate.Write(now);
    }

    public void AddNewData(DateTimeOffset dateTimeOffset, T data)
    {
        var rounded = dateTimeOffset.Round(SampleInterval);

        Interlocked.Increment(ref _dataCount);
        AddData(rounded, data);

        UpdateEnd(rounded);
        _lastDate.Write(rounded);
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
            empty.Merge(accumulator);
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

    protected void SetDataCount(ulong value)
    {
        Volatile.Write(ref _dataCount, unchecked((long)value));
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
        _start.Write(now);
        _end.Write(now);
    }

    private void UpdateOnAdd(DateTimeOffset key)
    {
        UpdateRange(key);
        EnsureCapacity();
    }

    private void UpdateEnd(DateTimeOffset key)
    {
        _end.TrySetLater(key);
        _start.TrySetEarlier(key);
    }

    private void UpdateRange(DateTimeOffset key)
    {
        _start.TrySetEarlier(key);
        _end.TrySetLater(key);
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
            _start.Write(now);
            _end.Write(now);
            return;
        }

        _start.Write(min);
        _end.Write(max);
    }

    private sealed class OrderedSampleView : IReadOnlyDictionary<DateTimeOffset, TSample>
    {
        private readonly ConcurrentDictionary<DateTimeOffset, TSample> _source;

        public OrderedSampleView(ConcurrentDictionary<DateTimeOffset, TSample> source)
        {
            _source = source;
        }

        public IEnumerable<DateTimeOffset> Keys => _source.Keys.OrderBy(static key => key);

        public IEnumerable<TSample> Values => _source.OrderBy(static pair => pair.Key).Select(static pair => pair.Value);

        public int Count => _source.Count;

        public TSample this[DateTimeOffset key] => _source[key];

        public bool ContainsKey(DateTimeOffset key) => _source.ContainsKey(key);

        public bool TryGetValue(DateTimeOffset key, out TSample value) => _source.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<DateTimeOffset, TSample>> GetEnumerator() => _source.OrderBy(static pair => pair.Key).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

internal struct AtomicDateTimeOffset
{
    private long _utcTicks;
    private int _offsetMinutes;

    public DateTimeOffset Read()
    {
        var ticks = Volatile.Read(ref _utcTicks);
        var offsetMinutes = Volatile.Read(ref _offsetMinutes);

        if (ticks == 0 && offsetMinutes == 0)
        {
            return default;
        }

        var utc = new DateTime(ticks == 0 ? 0 : ticks, DateTimeKind.Utc);
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        return new DateTimeOffset(utc, TimeSpan.Zero).ToOffset(offset);
    }

    public void Write(DateTimeOffset value)
    {
        Volatile.Write(ref _utcTicks, value.UtcTicks);
        Volatile.Write(ref _offsetMinutes, (int)value.Offset.TotalMinutes);
    }

    public void TrySetEarlier(DateTimeOffset candidate)
    {
        while (true)
        {
            var currentTicks = Volatile.Read(ref _utcTicks);
            if (currentTicks != 0 && candidate.UtcTicks >= currentTicks)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _utcTicks, candidate.UtcTicks, currentTicks) == currentTicks)
            {
                Volatile.Write(ref _offsetMinutes, (int)candidate.Offset.TotalMinutes);
                return;
            }
        }
    }

    public void TrySetLater(DateTimeOffset candidate)
    {
        while (true)
        {
            var currentTicks = Volatile.Read(ref _utcTicks);
            if (currentTicks != 0 && candidate.UtcTicks <= currentTicks)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _utcTicks, candidate.UtcTicks, currentTicks) == currentTicks)
            {
                Volatile.Write(ref _offsetMinutes, (int)candidate.Offset.TotalMinutes);
                return;
            }
        }
    }
}
