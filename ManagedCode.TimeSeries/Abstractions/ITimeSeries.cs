using System.Diagnostics.Contracts;

namespace ManagedCode.TimeSeries.Abstractions;

public interface ITimeSeries<in T, TSample, TSelf> : ITimeSeries<TSample>
    where TSelf : ITimeSeries<T, TSample, TSelf>
{
    void Resample(TimeSpan sampleInterval, int samplesCount);
    
    void AddNewData(T data);

    void Merge(TSelf accumulator);

    [Pure]
    TSelf PureMerge(IEnumerable<TSelf> accumulators);

    void Merge(IEnumerable<TSelf> accumulators);

    [Pure]
    TSelf PureMerge(TSelf accumulator);


    [Pure]
    static virtual TSelf operator +(TSelf series, TSelf right)
    {
        return series.PureMerge(right);
    }

    static virtual TSelf operator checked +(TSelf series, TSelf right)
    {
        return series + right;
    }

    // static virtual ITimeSeries<T, TSample> operator +(ITimeSeries<T, TSample> series, T value)
    // {
    //     series.AddNewData(value);
    //
    //     return series;
    // }

    // static virtual ITimeSeries<T, TSample> operator checked +(ITimeSeries<T, TSample> series, T value)
    // {
    //     return series + value;
    // }
}

public interface ITimeSeries<TSample> : IDisposable
{
    public Dictionary<DateTimeOffset, TSample> Samples { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public DateTimeOffset LastDate { get; }
    public TimeSpan SampleInterval { get; }
    public int SamplesCount { get; }
    public ulong DataCount { get; }
    public bool IsFull { get; }
    public bool IsEmpty { get; }
    public bool IsOverflow { get; }
}