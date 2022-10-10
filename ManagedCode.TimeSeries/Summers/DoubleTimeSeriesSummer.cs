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

    public virtual void Increment()
    {
        AddNewData(1);
    }

    public virtual void Decrement()
    {
        AddNewData(-1);
    }
    
    public double Average()
    {
        lock (_sync)
        {
            return Samples.Values.Average();
        }
    }
}