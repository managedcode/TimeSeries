namespace ManagedCode.TimeSeries;

/// <summary>
/// Controls how buckets are pre-created around the current range.
/// </summary>
public enum MarkupDirection
{
    /// <summary>
    /// Creates buckets both backward and forward from the current start.
    /// </summary>
    Middle = 0,
    /// <summary>
    /// Creates buckets backward from the current start.
    /// </summary>
    Past = 1,
    /// <summary>
    /// Obsolete alias for <see cref="Future"/>.
    /// </summary>
    [Obsolete("Use Future instead.")]
    Feature = 2,
    /// <summary>
    /// Creates buckets forward from the current start.
    /// </summary>
    Future = 2
}
