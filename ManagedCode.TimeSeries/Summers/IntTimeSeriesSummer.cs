namespace ManagedCode.TimeSeries.Summers;

public class IntTimeSeriesSummer : BaseTimeSeriesSummer<int>
{
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }

    protected override int Plus(int left, int right)
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
            if (Samples.Count == 0)
                return 0;
            
            return Samples.Values.Average();
        }
    }
}