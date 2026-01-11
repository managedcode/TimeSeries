using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using ManagedCode.TimeSeries.Abstractions;

namespace ManagedCode.TimeSeries.Serialization;

/// <summary>
/// System.Text.Json helpers for serializing and deserializing time-series snapshots.
/// </summary>
public static class TimeSeriesJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new();

    /// <summary>
    /// Serializes an accumulator to JSON.
    /// </summary>
    /// <param name="series">The accumulator to serialize.</param>
    /// <param name="options">Optional serializer options.</param>
    public static string SerializeAccumulator<T, TSelf>(TSelf series, JsonSerializerOptions? options = null)
        where TSelf : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf>
    {
        ArgumentNullException.ThrowIfNull(series);

        var snapshot = TimeSeriesAccumulatorSnapshot<T>.FromSeries(series);
        return JsonSerializer.Serialize(snapshot, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes an accumulator from JSON.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="options">Optional serializer options.</param>
    public static TSelf DeserializeAccumulator<T, TSelf>(string json, JsonSerializerOptions? options = null)
        where TSelf : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf>
    {
        ArgumentNullException.ThrowIfNull(json);

        var snapshot = JsonSerializer.Deserialize<TimeSeriesAccumulatorSnapshot<T>>(json, options ?? DefaultOptions)
            ?? throw new JsonException("Unable to deserialize accumulator snapshot.");

        return snapshot.ToSeries<TSelf>();
    }

    /// <summary>
    /// Serializes a numeric summer to JSON.
    /// </summary>
    /// <param name="series">The summer to serialize.</param>
    /// <param name="options">Optional serializer options.</param>
    public static string SerializeSummer<TNumber, TSelf>(TSelf series, JsonSerializerOptions? options = null)
        where TNumber : struct, INumber<TNumber>
        where TSelf : BaseNumberTimeSeriesSummer<TNumber, TSelf>
    {
        ArgumentNullException.ThrowIfNull(series);

        var snapshot = TimeSeriesSummerSnapshot<TNumber>.FromSeries(series);
        return JsonSerializer.Serialize(snapshot, options ?? DefaultOptions);
    }

    /// <summary>
    /// Deserializes a numeric summer from JSON.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="options">Optional serializer options.</param>
    public static TSelf DeserializeSummer<TNumber, TSelf>(string json, JsonSerializerOptions? options = null)
        where TNumber : struct, INumber<TNumber>
        where TSelf : BaseNumberTimeSeriesSummer<TNumber, TSelf>
    {
        ArgumentNullException.ThrowIfNull(json);

        var snapshot = JsonSerializer.Deserialize<TimeSeriesSummerSnapshot<TNumber>>(json, options ?? DefaultOptions)
            ?? throw new JsonException("Unable to deserialize summer snapshot.");

        return snapshot.ToSeries<TSelf>();
    }

    private static DateTime AsUtc(DateTimeOffset value) => DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Utc);

    private static DateTimeOffset AsOffset(DateTime value)
    {
        var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    private sealed class TimeSeriesAccumulatorSnapshot<T>
    {
        public Dictionary<DateTime, T[]> Samples { get; set; } = new();
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeSpan SampleInterval { get; set; }
        public int MaxSamplesCount { get; set; }
        public DateTime LastDate { get; set; }
        public ulong DataCount { get; set; }

        public static TimeSeriesAccumulatorSnapshot<T> FromSeries<TSelf>(BaseTimeSeries<T, ConcurrentQueue<T>, TSelf> series)
            where TSelf : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf>
        {
            var snapshot = new TimeSeriesAccumulatorSnapshot<T>
            {
                Start = AsUtc(series.Start),
                End = AsUtc(series.End),
                SampleInterval = series.SampleInterval,
                MaxSamplesCount = series.MaxSamplesCount,
                LastDate = AsUtc(series.LastDate),
                DataCount = series.DataCount
            };

            if (series.Samples.Count == 0)
            {
                return snapshot;
            }

            var samples = new Dictionary<DateTime, T[]>(series.Samples.Count);
            foreach (var pair in series.Samples)
            {
                samples[AsUtc(pair.Key)] = pair.Value.ToArray();
            }

            snapshot.Samples = samples;
            return snapshot;
        }

        public TSelf ToSeries<TSelf>() where TSelf : BaseTimeSeries<T, ConcurrentQueue<T>, TSelf>
        {
            var series = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount)!;
            var snapshotSamples = Samples ?? new Dictionary<DateTime, T[]>();
            var converted = new Dictionary<DateTimeOffset, ConcurrentQueue<T>>(snapshotSamples.Count);

            foreach (var pair in snapshotSamples)
            {
                converted[AsOffset(pair.Key)] = new ConcurrentQueue<T>(pair.Value);
            }

            series.InitInternal(converted, AsOffset(Start), AsOffset(End), AsOffset(LastDate), DataCount);
            return series;
        }
    }

    private sealed class TimeSeriesSummerSnapshot<TNumber>
        where TNumber : struct, INumber<TNumber>
    {
        public Dictionary<DateTime, TNumber> Samples { get; set; } = new();
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeSpan SampleInterval { get; set; }
        public int MaxSamplesCount { get; set; }
        public DateTime LastDate { get; set; }
        public ulong DataCount { get; set; }
        public Strategy Strategy { get; set; }

        public static TimeSeriesSummerSnapshot<TNumber> FromSeries<TSelf>(BaseNumberTimeSeriesSummer<TNumber, TSelf> series)
            where TSelf : BaseNumberTimeSeriesSummer<TNumber, TSelf>
        {
            var snapshot = new TimeSeriesSummerSnapshot<TNumber>
            {
                Start = AsUtc(series.Start),
                End = AsUtc(series.End),
                SampleInterval = series.SampleInterval,
                MaxSamplesCount = series.MaxSamplesCount,
                LastDate = AsUtc(series.LastDate),
                DataCount = series.DataCount,
                Strategy = series.Strategy
            };

            if (series.Samples.Count == 0)
            {
                return snapshot;
            }

            var samples = new Dictionary<DateTime, TNumber>(series.Samples.Count);
            foreach (var pair in series.Samples)
            {
                samples[AsUtc(pair.Key)] = pair.Value;
            }

            snapshot.Samples = samples;
            return snapshot;
        }

        public TSelf ToSeries<TSelf>() where TSelf : BaseNumberTimeSeriesSummer<TNumber, TSelf>
        {
            var series = (TSelf)Activator.CreateInstance(typeof(TSelf), SampleInterval, MaxSamplesCount, Strategy)!;
            var snapshotSamples = Samples ?? new Dictionary<DateTime, TNumber>();
            var converted = new Dictionary<DateTimeOffset, TNumber>(snapshotSamples.Count);

            foreach (var pair in snapshotSamples)
            {
                converted[AsOffset(pair.Key)] = pair.Value;
            }

            series.InitInternal(converted, AsOffset(Start), AsOffset(End), AsOffset(LastDate), DataCount);
            return series;
        }
    }
}
