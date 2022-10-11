namespace ManagedCode.TimeSeries;

public abstract class BaseGroupTimeSeriesSummer<T,TSummer> where TSummer : BaseTimeSeriesSummer<T>, IDisposable
{
    private readonly Strategy _strategy;
    private readonly bool _deleteOverdueSamples;
    private readonly Timer _timer;
    public readonly Dictionary<string, TSummer> TimeSeries = new();

    protected BaseGroupTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
    {
        if (deleteOverdueSamples)
        {
            _timer = new Timer(Callback, null, sampleInterval, sampleInterval);
        }
    }

    private void Callback(object? state)
    {
        foreach (var summer in TimeSeries.ToArray())
        {
            summer.Value.DeleteOverdueSamples();
            lock (TimeSeries)
            {
                if (summer.Value.IsEmpty)
                {
                    TimeSeries.Remove(summer.Key);
                }
            }
        }
    }

    public virtual void AddNewData(string key, T value)
    {
        lock (TimeSeries)
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
        lock (TimeSeries)
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
        lock (TimeSeries)
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

    public abstract T Average();
    
    public abstract T Min();
    
    public abstract T Max();
    
    public abstract T Sum();

    protected abstract TSummer CreateSummer();

    public void Dispose()
    {
        _timer?.Dispose();
    }

}