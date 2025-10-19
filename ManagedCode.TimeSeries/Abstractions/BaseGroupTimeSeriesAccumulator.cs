using System;
using System.Collections.Generic;
using System.Threading;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseGroupTimeSeriesAccumulator<T, TAccumulator> : IDisposable
    where TAccumulator : BaseTimeSeriesAccumulator<T, TAccumulator>
{
    private readonly Func<TAccumulator> _factory;
    private readonly Timer? _timer;
    protected Lock SyncRoot = new();
    protected readonly Dictionary<string, TAccumulator> Accumulators = new();

    protected BaseGroupTimeSeriesAccumulator(TimeSpan sampleInterval, bool deleteOverdueSamples, Func<TAccumulator> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (deleteOverdueSamples)
        {
            _timer = new Timer(Callback, null, sampleInterval, sampleInterval);
        }
    }

    private void Callback(object? state)
    {
        lock (SyncRoot)
        {
            if (Accumulators.Count == 0)
            {
                return;
            }

            var keys = new List<string>(Accumulators.Count);
            foreach (var pair in Accumulators)
            {
                keys.Add(pair.Key);
            }

            foreach (var key in keys)
            {
                if (!Accumulators.TryGetValue(key, out var accumulator))
                {
                    continue;
                }

                accumulator.DeleteOverdueSamples();
                if (accumulator.IsEmpty)
                {
                    Accumulators.Remove(key);
                }
            }
        }
    }

    protected TAccumulator CreateAccumulator()
    {
        return _factory();
    }

    public TAccumulator GetOrAdd(string key)
    {
        lock (SyncRoot)
        {
            if (!Accumulators.TryGetValue(key, out var accumulator))
            {
                accumulator = CreateAccumulator();
                Accumulators[key] = accumulator;
            }

            return accumulator;
        }
    }

    public void AddNewData(string key, T data)
    {
        GetOrAdd(key).AddNewData(data);
    }

    public void AddNewData(string key, DateTimeOffset dateTimeOffset, T data)
    {
        GetOrAdd(key).AddNewData(dateTimeOffset, data);
    }

    public bool TryGet(string key, out TAccumulator accumulator)
    {
        lock (SyncRoot)
        {
            return Accumulators.TryGetValue(key, out accumulator!);
        }
    }

    public bool Remove(string key)
    {
        lock (SyncRoot)
        {
            return Accumulators.Remove(key);
        }
    }

    public IReadOnlyList<KeyValuePair<string, TAccumulator>> Snapshot()
    {
        lock (SyncRoot)
        {
            if (Accumulators.Count == 0)
            {
                return Array.Empty<KeyValuePair<string, TAccumulator>>();
            }

            var copy = new List<KeyValuePair<string, TAccumulator>>(Accumulators.Count);
            foreach (var pair in Accumulators)
            {
                copy.Add(new KeyValuePair<string, TAccumulator>(pair.Key, pair.Value));
            }

            return copy;
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
