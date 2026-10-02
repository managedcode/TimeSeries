using System.Globalization;
using System.Text;
using System.Text.Json;

namespace ManagedCode.TimeSeries.Benchmark.Benchmarks;

internal static class TimeSeriesIngressEvidence
{
    private const string EvidenceMarker = "TIMESERIES_INGRESS_EVIDENCE";
    private const ulong FnvPrime = 1099511628211UL;

    public const ulong OffsetBasis = 14695981039346656037UL;

    public static void ValidateBuckets<TValue>(IReadOnlyDictionary<DateTimeOffset, TValue> buckets, int capacity)
    {
        if (buckets.Count > capacity)
        {
            throw new InvalidOperationException($"Retained bucket count {buckets.Count} exceeds configured capacity {capacity}.");
        }

        foreach (var key in buckets.Keys)
        {
            if (key.Offset != TimeSpan.Zero)
            {
                throw new InvalidOperationException($"Bucket key {key:O} is not UTC-normalized.");
            }
        }
    }

    public static void ValidateBatchDataCount(ulong seedDataCount, ulong dataCount, int operationsPerBatch)
    {
        if (dataCount <= seedDataCount)
        {
            throw new InvalidOperationException($"DataCount {dataCount} did not advance beyond seed {seedDataCount}.");
        }

        var addedDataCount = dataCount - seedDataCount;
        if (addedDataCount % (ulong)operationsPerBatch != 0)
        {
            throw new InvalidOperationException(
                $"DataCount {dataCount} did not advance beyond seed {seedDataCount} in batches of {operationsPerBatch}.");
        }
    }

    public static ulong Mix(ulong checksum, ulong value)
    {
        for (var index = 0; index < sizeof(ulong); index++)
        {
            checksum = (checksum ^ (byte)value) * FnvPrime;
            value >>= 8;
        }

        return checksum;
    }

    public static ulong MixText(ulong checksum, string value)
    {
        foreach (var valueByte in Encoding.UTF8.GetBytes(value))
        {
            checksum = (checksum ^ valueByte) * FnvPrime;
        }

        return checksum;
    }

    public static void Write(
        string workload,
        string seriesType,
        int capacity,
        ulong seedDataCount,
        ulong dataCount,
        int retainedBucketCount,
        ulong checksum)
    {
        var evidence = new
        {
            marker = EvidenceMarker,
            workload,
            seriesType,
            capacity,
            seedDataCount,
            dataCount,
            retainedBucketCount,
            checksum = checksum.ToString("X16", CultureInfo.InvariantCulture)
        };

        Console.WriteLine($"{EvidenceMarker} {JsonSerializer.Serialize(evidence)}");
    }
}
