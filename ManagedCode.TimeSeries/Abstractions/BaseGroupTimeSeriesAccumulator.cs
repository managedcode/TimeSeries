using System.Collections.Concurrent;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for keyed groups of accumulators.
/// </summary>
public abstract class BaseGroupTimeSeriesAccumulator<T, TAccumulator> : IDisposable
    where TAccumulator : BaseTimeSeriesAccumulator<T, TAccumulator>
{
    private readonly bool _deleteOverdueSamples;
    private readonly Func<TAccumulator> _factory;
    private readonly Timer? _timer;
    private readonly TimeSpan _sampleInterval;
    protected readonly ConcurrentDictionary<string, TAccumulator> Accumulators = new();

    protected BaseGroupTimeSeriesAccumulator(TimeSpan sampleInterval, bool deleteOverdueSamples, Func<TAccumulator> factory)
    {
        _sampleInterval = sampleInterval;
        _deleteOverdueSamples = deleteOverdueSamples;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (deleteOverdueSamples)
        {
            _timer = new Timer(Callback, null, sampleInterval, sampleInterval);
        }
    }

    /// <summary>
    /// Gets the bucket width used by the underlying accumulators.
    /// </summary>
    public TimeSpan SampleInterval => _sampleInterval;

    /// <summary>
    /// Gets whether overdue bucket cleanup is enabled.
    /// </summary>
    public bool DeleteOverdueSamplesEnabled => _deleteOverdueSamples;

    private void Callback(object? state)
    {
        foreach (var pair in Accumulators)
        {
            var accumulator = pair.Value;
            accumulator.DeleteOverdueSamples();
            if (accumulator.IsEmpty)
            {
                Accumulators.TryRemove(pair.Key, out _);
            }
        }
    }

    /// <summary>
    /// Creates an accumulator instance for a new key.
    /// </summary>
    protected TAccumulator CreateAccumulator()
    {
        return _factory();
    }

    /// <summary>
    /// Gets the accumulator for the key or creates one if missing.
    /// </summary>
    /// <param name="key">The group key.</param>
    public TAccumulator GetOrAdd(string key)
    {
        return Accumulators.GetOrAdd(key, _ => CreateAccumulator());
    }

    /// <summary>
    /// Adds a value to the accumulator for the specified key using the current time.
    /// </summary>
    /// <param name="key">The group key.</param>
    /// <param name="data">The value to add.</param>
    public void AddNewData(string key, T data)
    {
        GetOrAdd(key).AddNewData(data);
    }

    /// <summary>
    /// Adds a value to the accumulator for the specified key at the given time (normalized to UTC).
    /// </summary>
    /// <param name="key">The group key.</param>
    /// <param name="dateTimeOffset">The timestamp to use.</param>
    /// <param name="data">The value to add.</param>
    public void AddNewData(string key, DateTimeOffset dateTimeOffset, T data)
    {
        GetOrAdd(key).AddNewData(dateTimeOffset, data);
    }

    /// <summary>
    /// Tries to get an accumulator for the specified key.
    /// </summary>
    /// <param name="key">The group key.</param>
    /// <param name="accumulator">The accumulator for the key.</param>
    public bool TryGet(string key, out TAccumulator accumulator)
    {
        return Accumulators.TryGetValue(key, out accumulator!);
    }

    /// <summary>
    /// Removes an accumulator for the specified key.
    /// </summary>
    /// <param name="key">The group key.</param>
    public bool Remove(string key)
    {
        return Accumulators.TryRemove(key, out _);
    }

    /// <summary>
    /// Creates a snapshot of the current group contents.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, TAccumulator>> Snapshot()
    {
        if (Accumulators.IsEmpty)
        {
            return Array.Empty<KeyValuePair<string, TAccumulator>>();
        }

        var copy = new List<KeyValuePair<string, TAccumulator>>(Accumulators.Count);
        foreach (var pair in Accumulators)
        {
            copy.Add(pair);
        }

        return copy;
    }

    /// <summary>
    /// Disposes any cleanup timers.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
