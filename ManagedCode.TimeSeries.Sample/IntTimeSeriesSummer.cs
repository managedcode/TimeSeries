namespace ManagedCode.TimeSeries.Sample;

public class IntTimeSeriesSummer : BaseTimeSeriesSummer<int>
{
    public IntTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount = 0) : base(sampleInterval, samplesCount)
    {
    }

    protected override int Plus(int left, int right)
    {
        return left + right;
    }

    public void Increment()
    {
        AddNewData(1);
    }

    public void Decrement()
    {
        AddNewData(-1);
    }
}