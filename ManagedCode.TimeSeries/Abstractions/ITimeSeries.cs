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
}

public interface ITimeSeries<TSample>
{
    public Dictionary<DateTimeOffset, TSample> Samples { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }
    public DateTimeOffset LastDate { get; }
    public TimeSpan SampleInterval { get; }
    public int MaxSamplesCount { get; }
    public ulong DataCount { get; }
    public bool IsFull { get; }
    public bool IsEmpty { get; }
    public bool IsOverflow { get; }
}