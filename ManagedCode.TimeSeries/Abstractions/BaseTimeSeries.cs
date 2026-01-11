using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using ManagedCode.TimeSeries.Extensions;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for time-series storage with UTC-normalized bucket keys and common operations.
/// </summary>
public abstract class BaseTimeSeries<T, TSample, TSelf> :
    ITimeSeries<T, TSelf> where TSelf : BaseTimeSeries<T, TSample, TSelf>
{
    private const int DefaultSampleCount = 100;

    private readonly ConcurrentDictionary<DateTimeOffset, TSample> _samples = new();
    private readonly OrderedSampleView _orderedSamples;
    private AtomicDateTimeOffset _start;
    private AtomicDateTimeOffset _end;
    private AtomicDateTimeOffset _lastDate;
    private ulong _dataCount;
    private int _maxSamplesCount;

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount)
    {
        EnsureValidConfiguration(sampleInterval, maxSamplesCount);
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;
        _orderedSamples = new OrderedSampleView(_samples);

        var now = UtcNowRounded();
        _start.Write(now);
        _end.Write(now);
        _lastDate.Write(BaseTimeSeries<T, TSample, TSelf>.UtcNow());
    }

    protected BaseTimeSeries(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset lastDate)
    {
        EnsureValidConfiguration(sampleInterval, maxSamplesCount);
        SampleInterval = sampleInterval;
        MaxSamplesCount = maxSamplesCount;
        _orderedSamples = new OrderedSampleView(_samples);

        _start.Write(start.RoundUtc(SampleInterval));
        _end.Write(end.RoundUtc(SampleInterval));
        _lastDate.Write(BaseTimeSeries<T, TSample, TSelf>.NormalizeUtc(lastDate));
    }

    protected ConcurrentDictionary<DateTimeOffset, TSample> Storage => _samples;

    /// <summary>
    /// Gets an ordered view of buckets keyed by UTC timestamps.
    /// </summary>
    public IReadOnlyDictionary<DateTimeOffset, TSample> Samples => _orderedSamples;

    /// <summary>
    /// Gets an ordered view of buckets (alias of <see cref="Samples"/>).
    /// </summary>
    public IReadOnlyDictionary<DateTimeOffset, TSample> Buckets => _orderedSamples;

    /// <summary>
    /// Gets the earliest bucket timestamp (UTC).
    /// </summary>
    public DateTimeOffset Start
    {
        get => _start.Read();
        internal set => _start.Write(value);
    }

    /// <summary>
    /// Gets the latest bucket timestamp (UTC).
    /// </summary>
    public DateTimeOffset End
    {
        get => _end.Read();
        protected set => _end.Write(value);
    }

    /// <summary>
    /// Gets the most recent event timestamp (UTC).
    /// </summary>
    public DateTimeOffset LastDate
    {
        get => _lastDate.Read();
        protected set => _lastDate.Write(value);
    }

    /// <summary>
    /// Gets the bucket width used for rounding timestamps.
    /// </summary>
    public TimeSpan SampleInterval { get; protected set; }

    /// <summary>
    /// Gets the maximum number of buckets to retain. Use 0 for unbounded.
    /// </summary>
    public int MaxSamplesCount
    {
        get => Volatile.Read(ref _maxSamplesCount);
        protected set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Max samples count must be non-negative.");
            }

            Volatile.Write(ref _maxSamplesCount, value);
        }
    }

    /// <summary>
    /// Gets the total number of events added (not the bucket count).
    /// </summary>
    public ulong DataCount => Volatile.Read(ref _dataCount);

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

        _start.Write(BaseTimeSeries<T, TSample, TSelf>.NormalizeUtc(start));
        _end.Write(BaseTimeSeries<T, TSample, TSelf>.NormalizeUtc(end));
        _lastDate.Write(BaseTimeSeries<T, TSample, TSelf>.NormalizeUtc(lastDate));
        Volatile.Write(ref _dataCount, dataCount);
    }

    /// <summary>
    /// Gets whether the bucket count has reached <see cref="MaxSamplesCount"/>.
    /// </summary>
    public bool IsFull => MaxSamplesCount > 0 && _samples.Count >= MaxSamplesCount;

    /// <summary>
    /// Gets whether there are no buckets.
    /// </summary>
    public bool IsEmpty => _samples.IsEmpty;

    /// <summary>
    /// Gets whether the bucket count exceeds <see cref="MaxSamplesCount"/>.
    /// </summary>
    public bool IsOverflow => MaxSamplesCount > 0 && _samples.Count > MaxSamplesCount;

    /// <summary>
    /// Re-buckets the series into a larger interval.
    /// </summary>
    /// <param name="sampleInterval">The new bucket width. Must be larger than the current interval.</param>
    /// <param name="samplesCount">The new maximum bucket count. Use 0 for unbounded.</param>
    public abstract void Resample(TimeSpan sampleInterval, int samplesCount);

    /// <summary>
    /// Creates an empty series instance with the provided configuration.
    /// </summary>
    /// <param name="sampleInterval">Bucket width. Must be positive.</param>
    /// <param name="maxSamplesCount">Maximum bucket count. Use 0 for unbounded.</param>
    public static TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0)
    {
        if (sampleInterval is null || sampleInterval.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be a positive value.");
        }

        if (maxSamplesCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSamplesCount), "Max samples count must be non-negative.");
        }

        return (TSelf)Activator.CreateInstance(typeof(TSelf), sampleInterval.Value, maxSamplesCount)!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Merges another series into the current instance.
    /// </summary>
    /// <param name="accumulator">The series to merge.</param>
    public abstract void Merge(TSelf accumulator);

    /// <summary>
    /// Adds a value using the current UTC timestamp.
    /// </summary>
    /// <param name="data">The value to add.</param>
    public void AddNewData(T data)
    {
        var now = DateTime.UtcNow;
        var rounded = RoundUtc(now);

        Interlocked.Increment(ref _dataCount);
        AddData(rounded, data);

        UpdateEnd(rounded);
        _lastDate.Write(new DateTimeOffset(now, TimeSpan.Zero));
    }

    /// <summary>
    /// Adds a value at the specified timestamp (normalized to UTC).
    /// </summary>
    /// <param name="dateTimeOffset">The timestamp to use.</param>
    /// <param name="data">The value to add.</param>
    public void AddNewData(DateTimeOffset dateTimeOffset, T data)
    {
        var normalized = BaseTimeSeries<T, TSample, TSelf>.NormalizeUtc(dateTimeOffset);
        var rounded = RoundUtc(normalized);

        Interlocked.Increment(ref _dataCount);
        AddData(rounded, data);

        UpdateEnd(rounded);
        _lastDate.Write(normalized);
    }

    /// <summary>
    /// Pre-creates buckets relative to the current <see cref="Start"/> value.
    /// </summary>
    /// <param name="direction">The direction to create buckets.</param>
    public void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past)
    {
        var samples = MaxSamplesCount > 0 ? MaxSamplesCount : DefaultSampleCount;

        if (direction is MarkupDirection.Past or MarkupDirection.Future)
        {
            var cursor = Start;
            for (var i = 0; i < samples; i++)
            {
                cursor = cursor.RoundUtc(SampleInterval);
                _ = GetOrCreateSample(cursor, static () => Activator.CreateInstance<TSample>()!);
                cursor = direction is MarkupDirection.Future ? cursor.Add(SampleInterval) : cursor.Subtract(SampleInterval);
            }
        }
        else
        {
            var forward = Start;
            var backward = Start;

            for (var i = 0; i < samples / 2 + 1; i++)
            {
                forward = forward.RoundUtc(SampleInterval);
                backward = backward.RoundUtc(SampleInterval);

                _ = GetOrCreateSample(forward, static () => Activator.CreateInstance<TSample>()!);
                _ = GetOrCreateSample(backward, static () => Activator.CreateInstance<TSample>()!);

                forward = forward.Add(SampleInterval);
                backward = backward.Subtract(SampleInterval);
            }
        }
    }

    /// <summary>
    /// Deletes buckets that fall outside the configured window.
    /// </summary>
    public void DeleteOverdueSamples()
    {
        if (MaxSamplesCount <= 0 || _samples.IsEmpty)
        {
            return;
        }

        var threshold = UtcNowRounded();
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

    /// <summary>
    /// Creates a new series and merges the supplied series into it.
    /// </summary>
    /// <param name="accumulators">The series to merge.</param>
    public TSelf Rebase(IEnumerable<TSelf> accumulators)
    {
        var empty = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;

        foreach (var accumulator in accumulators)
        {
            empty.Merge(accumulator);
        }

        return empty;
    }

    /// <summary>
    /// Merges the supplied series into the current instance.
    /// </summary>
    /// <param name="accumulators">The series to merge.</param>
    public void Merge(IEnumerable<TSelf> accumulators)
    {
        foreach (var accumulator in accumulators)
        {
            Merge(accumulator);
        }
    }

    /// <summary>
    /// Creates a new series and merges the supplied series into it.
    /// </summary>
    /// <param name="accumulator">The series to merge.</param>
    public TSelf Rebase(TSelf accumulator)
    {
        var empty = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;
        empty.Merge(accumulator);

        return empty;
    }

    /// <summary>
    /// Creates a new series which merges both operands.
    /// </summary>
    public static TSelf operator +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        var merged = left.Rebase(right);
        merged.Merge((TSelf)left);
        return merged;
    }

    /// <summary>
    /// Creates a new series which merges both operands with checked arithmetic.
    /// </summary>
    public static TSelf operator checked +(BaseTimeSeries<T, TSample, TSelf> left, TSelf right)
    {
        var merged = left.Rebase(right);
        merged.Merge((TSelf)left);
        return merged;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void AddData(DateTimeOffset now, T data);

    /// <summary>
    /// Adds to the data counter without changing bucket contents.
    /// </summary>
    /// <param name="value">The amount to add.</param>
    protected void AddToDataCount(ulong value)
    {
        if (value == 0)
        {
            return;
        }

        Interlocked.Add(ref _dataCount, value);
    }

    /// <summary>
    /// Sets the data counter without changing bucket contents.
    /// </summary>
    /// <param name="value">The new counter value.</param>
    protected void SetDataCount(ulong value)
    {
        Volatile.Write(ref _dataCount, value);
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
        var now = UtcNowRounded();
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

        var overflow = _samples.Count - MaxSamplesCount;
        if (overflow <= 0)
        {
            return;
        }

        var keysToRemove = _samples.Keys.OrderBy(static key => key).Take(overflow).ToArray();
        var removedAny = false;

        foreach (var key in keysToRemove)
        {
            if (_samples.TryRemove(key, out _))
            {
                removedAny = true;
            }
        }

        if (removedAny)
        {
            RecalculateRange();
        }
    }

    protected void RecalculateRange()
    {
        var min = DateTimeOffset.MaxValue;
        var max = DateTimeOffset.MinValue;

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
            var now = UtcNowRounded();
            _start.Write(now);
            _end.Write(now);
            return;
        }

        _start.Write(min);
        _end.Write(max);
    }

    private sealed class OrderedSampleView(ConcurrentDictionary<DateTimeOffset, TSample> source) : IReadOnlyDictionary<DateTimeOffset, TSample>
    {
        private readonly ConcurrentDictionary<DateTimeOffset, TSample> _source = source;

        public IEnumerable<DateTimeOffset> Keys => _source.Keys.OrderBy(static key => key);

        public IEnumerable<TSample> Values => _source.OrderBy(static pair => pair.Key).Select(static pair => pair.Value);

        public int Count => _source.Count;

        public TSample this[DateTimeOffset key] => _source[key];

        public bool ContainsKey(DateTimeOffset key) => _source.ContainsKey(key);

        public bool TryGetValue(DateTimeOffset key, out TSample value)
        {
            var found = _source.TryGetValue(key, out var existing);
            value = existing!;
            return found;
        }

        public IEnumerator<KeyValuePair<DateTimeOffset, TSample>> GetEnumerator() => _source.OrderBy(static pair => pair.Key).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static void EnsureValidConfiguration(TimeSpan sampleInterval, int maxSamplesCount)
    {
        if (sampleInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleInterval), "Sample interval must be positive.");
        }

        if (maxSamplesCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSamplesCount), "Max samples count must be non-negative.");
        }
    }

    private static DateTimeOffset NormalizeUtc(DateTimeOffset value) => new(value.UtcDateTime, TimeSpan.Zero);

    private DateTimeOffset RoundUtc(DateTimeOffset value) => value.RoundUtc(SampleInterval);

    private DateTimeOffset RoundUtc(DateTime value) => new(value.Round(SampleInterval), TimeSpan.Zero);

    private static DateTimeOffset UtcNow() => new(DateTime.UtcNow, TimeSpan.Zero);

    private DateTimeOffset UtcNowRounded() => RoundUtc(DateTime.UtcNow);
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
