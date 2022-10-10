namespace ManagedCode.TimeSeries.Summers;

public class IntGroupTimeSeriesSummer : BaseGroupTimeSeriesSummer<int, IntTimeSeriesSummer>
{
    private readonly TimeSpan _sampleInterval;
    private readonly int _samplesCount;

    public IntGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, bool deleteOverdueSamples) : base(sampleInterval, deleteOverdueSamples)
    {
        _sampleInterval = sampleInterval;
        _samplesCount = samplesCount;
    } 
    
    public override int Average()
    {
        lock (TimeSeries)
        {
            if (TimeSeries.Count == 0)
                return 0;
            
            return (int)Math.Round(TimeSeries.Select(s => s.Value.Average()).Average());
        }
    }

    protected override IntTimeSeriesSummer CreateSummer()
    {
        return new IntTimeSeriesSummer(_sampleInterval, _samplesCount);
    }
}