using System.Collections.Concurrent;
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
    public readonly ConcurrentDictionary<string, TSummer> TimeSeries = new();

    protected BaseGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
    {
        _timer = deleteOverdueSamples ? new Timer(Callback, null, sampleInterval, sampleInterval) : null;
    }

    private void Callback(object? state)
    {
        foreach (var (key, summer) in TimeSeries.ToArray())
        {
            summer.DeleteOverdueSamples();
            if (summer.IsEmpty)
            {
                TimeSeries.TryRemove(key, out _);
            }
        }
    }

    public virtual void AddNewData(string key, TNumber value)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.AddNewData(value);
    }

    public virtual void Increment(string key)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.Increment();
    }

    public virtual void Decrement(string key)
    {
        var summer = TimeSeries.GetOrAdd(key, _ => CreateSummer());
        summer.Decrement();
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
