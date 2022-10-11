namespace ManagedCode.TimeSeries.Summers;

public class FloatTimeSeriesSummer : BaseTimeSeriesSummer<float>
{
    public FloatTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount, Strategy strategy) : base(sampleInterval, samplesCount, strategy)
    {
    }
    
    public FloatTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount) : base(sampleInterval, samplesCount, Strategy.Sum)
    {
    }
    
    public FloatTimeSeriesSummer(TimeSpan sampleInterval) : base(sampleInterval, 0, Strategy.Sum)
    {
    }

    protected override float Plus(float left, float right)
    {
        return left + right;
    }
    
    protected override float Min(float left, float right)
    {
        return Math.Min(left, right);
    }

    protected override float Max(float left, float right)
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
    
    public override float Average()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Average();
        }
    }
    
    public override float Sum()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Sum();
        }
    }

    public override float Min()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Min();
        }
    }
    
    public override float Max()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Max();
        }
    }
}