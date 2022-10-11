namespace ManagedCode.TimeSeries.Summers;

public class DoubleTimeSeriesSummer : BaseTimeSeriesSummer<double>
{
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount, strategy)
    {
    }
    
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount, Strategy.Sum)
    {
    }
    
    public DoubleTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }

    protected override double Plus(double left, double right)
    {
        return left + right;
    }

    protected override double Min(double left, double right)
    {
        return Math.Min(left, right);
    }

    protected override double Max(double left, double right)
    {
        return Math.Max(left, right);
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

    public override double Sum()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Sum();
        }
    }

    public override double Min()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Min();
        }
    }
    
    public override double Max()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Max();
        }
    }
}