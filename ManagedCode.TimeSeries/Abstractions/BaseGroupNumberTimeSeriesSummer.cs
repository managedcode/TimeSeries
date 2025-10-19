using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace ManagedCode.TimeSeries.Abstractions;

public abstract class BaseGroupNumberTimeSeriesSummer<TNumber, TSummer, TSelf> : IDisposable
    where TSummer : BaseNumberTimeSeriesSummer<TNumber, TSummer>
    where TNumber : struct, INumber<TNumber>
    where TSelf : BaseGroupNumberTimeSeriesSummer<TNumber, TSummer, TSelf>
{
    internal readonly Timer? _timer;
    protected Lock SyncRoot = new();
    public readonly Dictionary<string, TSummer> TimeSeries = new();

    protected BaseGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
    {
        _timer = deleteOverdueSamples ? new Timer(Callback, null, sampleInterval, sampleInterval) : null;
    }

    private void Callback(object? state)
    {
        lock (SyncRoot)
        {
            if (TimeSeries.Count == 0)
            {
                return;
            }

            var keys = new List<string>(TimeSeries.Count);
            foreach (var pair in TimeSeries)
            {
                keys.Add(pair.Key);
            }

            foreach (var key in keys)
            {
                if (!TimeSeries.TryGetValue(key, out var summer))
                {
                    continue;
                }

                summer.DeleteOverdueSamples();
                if (summer.IsEmpty)
                {
                    TimeSeries.Remove(key);
                }
            }
        }
    }

    public virtual void AddNewData(string key, TNumber value)
    {
        lock (SyncRoot)
        {
            if (TimeSeries.TryGetValue(key, out var summer))
            {
                summer.AddNewData(value);
            }
            else
            {
                var newSummer = CreateSummer();
                newSummer.AddNewData(value);
                TimeSeries[key] = newSummer;
            }
        }
    }

    public virtual void Increment(string key)
    {
        lock (SyncRoot)
        {
            if (TimeSeries.TryGetValue(key, out var summer))
            {
                summer.Increment();
            }
            else
            {
                var newSummer = CreateSummer();
                newSummer.Increment();
                TimeSeries[key] = newSummer;
            }
        }
    }

    public virtual void Decrement(string key)
    {
        lock (SyncRoot)
        {
            if (TimeSeries.TryGetValue(key, out var summer))
            {
                summer.Decrement();
            }
            else
            {
                var newSummer = CreateSummer();
                newSummer.Decrement();
                TimeSeries[key] = newSummer;
            }
        }
    }

    public abstract TNumber Average();

    public abstract TNumber Min();

    public abstract TNumber Max();

    public abstract TNumber Sum();

    protected abstract TSummer CreateSummer();

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
