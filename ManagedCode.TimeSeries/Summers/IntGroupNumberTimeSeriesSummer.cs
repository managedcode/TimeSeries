using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Summers;

public class IntGroupNumberTimeSeriesSummer : BaseGroupNumberTimeSeriesSummer<int, IntTimeSeriesSummer, IntGroupNumberTimeSeriesSummer>
{
    private readonly TimeSpan _sampleInterval;
    private readonly int _samplesCount;
    private readonly Strategy _strategy;

    public IntGroupNumberTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount,  Strategy strategy, bool deleteOverdueSamples) : base(sampleInterval, deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _samplesCount = samplesCount;
        _strategy = strategy;
    } 

    public override int Average()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return Convert.ToInt32(Math.Round(TimeSeries.Select(s => s.Value.Average()).Average()));
        }
    }

    public override int Min()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return TimeSeries.Select(s => s.Value.Min()).Min();
        }
    }

    public override int Max()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return TimeSeries.Select(s => s.Value.Max()).Max();
        }
    }

    public override int Sum()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return TimeSeries.Select(s => s.Value.Sum()).Sum();
        }
    }

    protected override IntTimeSeriesSummer CreateSummer()
    {
        return new IntTimeSeriesSummer(_sampleInterval, _samplesCount, _strategy);
    }
}