using System.Diagnostics.Contracts;

namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Defines common time-series operations for adding, merging, and resampling data.
/// </summary>
public interface ITimeSeries<in T, TSelf> where TSelf : ITimeSeries<T, TSelf>
{
    /// <summary>
    /// Adds a value using the current UTC timestamp.
    /// </summary>
    /// <param name="data">The value to add.</param>
    void AddNewData(T data);

    /// <summary>
    /// Adds a value at the specified timestamp (normalized to UTC).
    /// </summary>
    /// <param name="dateTimeOffset">The timestamp to use.</param>
    /// <param name="data">The value to add.</param>
    void AddNewData(DateTimeOffset dateTimeOffset, T data);

    /// <summary>
    /// Deletes buckets which fall outside the configured window.
    /// </summary>
    void DeleteOverdueSamples();

    /// <summary>
    /// Pre-creates buckets relative to the current range.
    /// </summary>
    /// <param name="direction">The direction to create buckets.</param>
    void MarkupAllSamples(MarkupDirection direction = MarkupDirection.Past);

    /// <summary>
    /// Creates a new series and merges the supplied series into it.
    /// </summary>
    /// <param name="accumulator">The series to merge.</param>
    [Pure]
    TSelf Rebase(TSelf accumulator);

    /// <summary>
    /// Creates a new series and merges the supplied series into it.
    /// </summary>
    /// <param name="accumulators">The series to merge.</param>
    [Pure]
    TSelf Rebase(IEnumerable<TSelf> accumulators);

    /// <summary>
    /// Merges the supplied series into the current instance.
    /// </summary>
    /// <param name="accumulator">The series to merge.</param>
    void Merge(TSelf accumulator);

    /// <summary>
    /// Merges the supplied series into the current instance.
    /// </summary>
    /// <param name="accumulators">The series to merge.</param>
    void Merge(IEnumerable<TSelf> accumulators);

    /// <summary>
    /// Re-buckets the series into a larger interval.
    /// </summary>
    /// <param name="sampleInterval">The new bucket width. Must be larger than the current interval.</param>
    /// <param name="samplesCount">The new maximum bucket count. Use 0 for unbounded.</param>
    void Resample(TimeSpan sampleInterval, int samplesCount);

    /// <summary>
    /// Creates an empty series instance with the provided configuration.
    /// </summary>
    /// <param name="sampleInterval">Bucket width. Must be positive.</param>
    /// <param name="maxSamplesCount">Maximum bucket count. Use 0 for unbounded.</param>
    [Pure]
    static abstract TSelf Empty(TimeSpan? sampleInterval = null, int maxSamplesCount = 0);
}
