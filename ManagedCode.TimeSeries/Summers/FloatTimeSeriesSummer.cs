namespace ManagedCode.TimeSeries.Summers;

public class FloatTimeSeriesSummer : BaseTimeSeriesSummer<float>
{
    public FloatTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }

    protected override float Plus(float left, float right)
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
    
    public override float Average()
    {
        lock (_sync)
        {
            if (Samples.Count == 0)
                return 0;
            
            return Convert.ToSingle(Samples.Values.Average());
        }
    }
}