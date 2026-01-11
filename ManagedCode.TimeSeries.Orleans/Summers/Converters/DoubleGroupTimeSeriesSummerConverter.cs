using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Orleans;

/// <summary>
/// Orleans converter for grouped double summers.
/// </summary>
[RegisterConverter]
public sealed class DoubleGroupTimeSeriesSummerConverter : IConverter<DoubleGroupTimeSeriesSummer, TimeSeriesGroupSummerSurrogate<double>>
{
    /// <inheritdoc />
    public DoubleGroupTimeSeriesSummer ConvertFromSurrogate(in TimeSeriesGroupSummerSurrogate<double> surrogate)
    {
        var group = new DoubleGroupTimeSeriesSummer(surrogate.SampleInterval, surrogate.MaxSamplesCount, surrogate.Strategy, surrogate.DeleteOverdueSamples);

        if (surrogate.Series.Count == 0)
        {
            return group;
        }

        var converter = new NumberTimeSeriesSummerConverter<double>();
        foreach (var pair in surrogate.Series)
        {
            var restored = converter.ConvertFromSurrogate(pair.Value);
            group.TimeSeries.TryAdd(pair.Key, restored);
        }

        return group;
    }

    /// <inheritdoc />
    public TimeSeriesGroupSummerSurrogate<double> ConvertToSurrogate(in DoubleGroupTimeSeriesSummer value)
    {
        var series = new Dictionary<string, TimeSeriesSummerSurrogate<double>>(value.TimeSeries.Count);
        var converter = new NumberTimeSeriesSummerConverter<double>();

        foreach (var pair in value.TimeSeries)
        {
            series[pair.Key] = converter.ConvertToSurrogate(pair.Value);
        }

        return new TimeSeriesGroupSummerSurrogate<double>(series, value.SampleInterval, value.MaxSamplesCount, value.Strategy, value.DeleteOverdueSamplesEnabled);
    }
}
