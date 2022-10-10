namespace ManagedCode.TimeSeries.Summers;

public class FloatGroupTimeSeriesSummer : BaseGroupTimeSeriesSummer<float, FloatTimeSeriesSummer>
{
    private readonly TimeSpan _sampleInterval;
    private readonly int _samplesCount;
    private readonly Strategy _strategy;

    public FloatGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount,  Strategy strategy, bool deleteOverdueSamples) : base(sampleInterval, deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _samplesCount = samplesCount;
        _strategy = strategy;
    } 
    
    public override float Average()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return Convert.ToSingle(TimeSeries.Select(s => s.Value.Average()).Average());
        }
    }

    protected override FloatTimeSeriesSummer CreateSummer()
    {
        return new FloatTimeSeriesSummer(_sampleInterval, _samplesCount, _strategy);
    }
}