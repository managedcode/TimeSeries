namespace ManagedCode.TimeSeries.Summers;

public class IntTimeSeriesSummer : BaseTimeSeriesSummer<int>
{
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount, strategy)
    {
    }
    
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount, Strategy.Sum)
    {
    }
    
    public IntTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }

    protected override int Plus(int left, int right)
    {
        return left + right;
    }
    
    protected override int Min(int left, int right)
    {
        return Math.Min(left, right);
    }

    protected override int Max(int left, int right)
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

    public override int Average()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return (int)Math.Round(Samples.Values.Average());
        }
    }
    
    public override int Sum()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Sum();
        }
    }

    public override int Min()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Min();
        }
    }
    
    public override int Max()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Max();
        }
    }
}