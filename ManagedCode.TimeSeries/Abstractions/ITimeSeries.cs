using System.Diagnostics.Contracts;

namespace ManagedCode.TimeSeries.Abstractions;

public interface ITimeSeries<in T, TSelf> where TSelf : ITimeSeries<T, TSelf>
{
    void AddNewData(T data);
    void AddNewData(DateTimeOffset dateTimeOffset, T data);

    void DeleteOverdueSamples();
    void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past);

    [Pure]
    TSelf Rebase(TSelf accumulator);

    [Pure]
    TSelf Rebase(IEnumerable<TSelf> accumulators);

    void Merge(TSelf accumulator);
    void Merge(IEnumerable<TSelf> accumulators);

    void Resample(TimeSpan sampleInterval, int samplesCount);

    [Pure]
    static abstract TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0);
}