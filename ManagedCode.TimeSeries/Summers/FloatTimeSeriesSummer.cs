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