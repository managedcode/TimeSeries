namespace ManagedCode.TimeSeries.Summers;

public class DoubleGroupTimeSeriesSummer : BaseGroupTimeSeriesSummer<double, DoubleTimeSeriesSummer>
{
    private readonly TimeSpan _sampleInterval;
    private readonly int _samplesCount;

    public DoubleGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, bool deleteOverdueSamples) : base(sampleInterval, deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _samplesCount = samplesCount;
    } 
    
    public override double Average()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return TimeSeries.Select(s => s.Value.Average()).Average();
        }
    }

    protected override DoubleTimeSeriesSummer CreateSummer()
    {
        return new DoubleTimeSeriesSummer(_sampleInterval, _samplesCount);
    }
}