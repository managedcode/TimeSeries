using System.Collections.Concurrent;
using System.Numerics;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Base class for keyed groups of numeric summers.
/// </summary>
public abstract class BaseGroupNumberTimeSeriesSummer<TNumber, TSummer, TSelf> : IDisposable
    where TSummer : BaseNumberTimeSeriesSummer<TNumber, TSummer>
    where TNumber : struct, INumber<TNumber>
    where TSelf : BaseGroupNumberTimeSeriesSummer<TNumber, TSummer, TSelf>
{
    internal readonly Timer? _timer;
    private readonly bool _deleteOverdueSamples;
    private readonly TimeSpan _sampleInterval;
    /// <summary>
    /// Stores the per-key summers for this group.
    /// </summary>
    public readonly ConcurrentDictionary<string, TSummer> TimeSeries = new();

    protected BaseGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _deleteOverdueSamples = deleteOverdueSamples;
        _timer = deleteOverdueSamples ? new Timer(Callback, null, sampleInterval, sampleInterval) : null;
    }

    /// <summary>
    /// Gets the bucket width used by the underlying summers.
    /// </summary>
    public TimeSpan SampleInterval => _sampleInterval;

    /// <summary>
    /// Gets whether overdue bucket cleanup is enabled.
    /// </summary>
    public bool DeleteOverdueSamplesEnabled => _deleteOverdueSamples;

    private void Callback(object? state)
    {
        foreach (var pair in TimeSeries)
        {
            var summer = pair.Value;
            summer.DeleteOverdueSamples();
            if (summer.IsEmpty)
            {
                TimeSeries.TryRemove(pair.Key, out _);
            }
        }
    }

    /// <summary>
    /// Adds a value to the summer for the specified key using the current time.
    /// </summary>
    /// <param name="key">The group key.</param>
    /// <param name="value">The value to add.</param>
    public virtual void AddNewData(string key, TNumber value)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.AddNewData(value);
    }

    /// <summary>
    /// Increments the current bucket for the specified key.
    /// </summary>
    /// <param name="key">The group key.</param>
    public virtual void Increment(string key)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.Increment();
    }

    /// <summary>
    /// Decrements the current bucket for the specified key.
    /// </summary>
    /// <param name="key">The group key.</param>
    public virtual void Decrement(string key)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.Decrement();
    }

    /// <summary>
    /// Returns the average across all buckets for all keys.
    /// </summary>
    public abstract TNumber Average();

    /// <summary>
    /// Returns the minimum value across all buckets for all keys.
    /// </summary>
    public abstract TNumber Min();

    /// <summary>
    /// Returns the maximum value across all buckets for all keys.
    /// </summary>
    public abstract TNumber Max();

    /// <summary>
    /// Returns the sum across all buckets for all keys.
    /// </summary>
    public abstract TNumber Sum();

    /// <summary>
    /// Creates a summer instance for a new key.
    /// </summary>
    protected abstract TSummer CreateSummer();

    /// <summary>
    /// Disposes any cleanup timers.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
