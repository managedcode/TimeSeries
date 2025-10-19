using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseGroupTimeSeriesAccumulator<T, TAccumulator> : IDisposable
    where TAccumulator : BaseTimeSeriesAccumulator<T, TAccumulator>
{
    private readonly Func<TAccumulator> _factory;
    private readonly Timer? _timer;
    protected readonly ConcurrentDictionary<string, TAccumulator> Accumulators = new();

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
        foreach (var (key, accumulator) in Accumulators.ToArray())
        {
            accumulator.DeleteOverdueSamples();
            if (accumulator.IsEmpty)
            {
                Accumulators.TryRemove(key, out _);
            }
        }
    }

    protected TAccumulator CreateAccumulator()
    {
        return _factory();
    }

    public TAccumulator GetOrAdd(string key)
    {
        return Accumulators.GetOrAdd(key, _ => CreateAccumulator());
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
        return Accumulators.TryGetValue(key, out accumulator!);
    }

    public bool Remove(string key)
    {
        return Accumulators.TryRemove(key, out _);
    }

    public IReadOnlyList<KeyValuePair<string, TAccumulator>> Snapshot()
    {
        if (Accumulators.IsEmpty)
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

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
