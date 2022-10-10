namespace ManagedCode.TimeSeries.Summers;

public class DoubleTimeSeriesSummer : BaseTimeSeriesSummer<double>
{
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }

    protected override double Plus(double left, double right)
    {
        return left + right;
    }

    public override void Increment()
    {
        AddNewData(1);
    }

    public override void Decrement()
    {
        AddNewData(-1);
    }
    
    public override double Average()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Average();
        }
    }
}