using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Extensions;

/// <summary>
/// Convenience extensions for recording values into time-series instances.
/// </summary>
public static class TimeSeriesExtensions
{
    /// <summary>
    /// Records a value using the current UTC timestamp.
    /// </summary>
    /// <param name="series">The target series.</param>
    /// <param name="data">The value to record.</param>
    public static void Record<T, TSelf>(this ITimeSeries<T, TSelf> series, T data)
        where TSelf : ITimeSeries<T, TSelf>
    {
        ArgumentNullException.ThrowIfNull(series);
        series.AddNewData(data);
    }

    /// <summary>
    /// Records a value at the specified timestamp (normalized to UTC).
    /// </summary>
    /// <param name="series">The target series.</param>
    /// <param name="timestamp">The timestamp to use.</param>
    /// <param name="data">The value to record.</param>
    public static void RecordAt<T, TSelf>(this ITimeSeries<T, TSelf> series, DateTimeOffset timestamp, T data)
        where TSelf : ITimeSeries<T, TSelf>
    {
        ArgumentNullException.ThrowIfNull(series);
        series.AddNewData(timestamp, data);
    }
}
