namespace ManagedCode.TimeSeries.Abstractions;

/// <summary>
/// Defines aggregation behavior for summers.
/// </summary>
public enum Strategy
{
    /// <summary>
    /// Adds values into the bucket.
    /// </summary>
    Sum,
    /// <summary>
    /// Keeps the minimum value per bucket.
    /// </summary>
    Min,
    /// <summary>
    /// Keeps the maximum value per bucket.
    /// </summary>
    Max,
    /// <summary>
    /// Replaces the bucket value with the latest value.
    /// </summary>
    Replace,
}
