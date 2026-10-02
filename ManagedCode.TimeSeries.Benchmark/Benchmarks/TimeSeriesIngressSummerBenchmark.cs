using System.Globalization;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Benchmark.Benchmarks;

[GenericTypeArguments(typeof(int))]
[GenericTypeArguments(typeof(long))]
[GenericTypeArguments(typeof(double))]
[GenericTypeArguments(typeof(decimal))]
[MemoryDiagnoser]
public class TimeSeriesIngressSummerBenchmark<TNumber>
    where TNumber : struct, INumber<TNumber>
{
    private const int OperationsPerBatch = 64;
    private const int ExistingBucketCount = 64;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
    private static readonly DateTimeOffset FirstBucket = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private NumberTimeSeriesSummer<TNumber> _summer = null!;
    private DateTimeOffset[] _bucketTimes = null!;
    private TNumber[] _values = null!;
    private ulong _seedDataCount;

    [GlobalSetup]
    public void Setup()
    {
        _summer = new NumberTimeSeriesSummer<TNumber>(SampleInterval, ExistingBucketCount, Strategy.Sum);
        _bucketTimes = new DateTimeOffset[ExistingBucketCount];
        _values = new TNumber[OperationsPerBatch];

        for (var index = 0; index < ExistingBucketCount; index++)
        {
            var timestamp = FirstBucket.AddTicks(index * SampleInterval.Ticks);
            _bucketTimes[index] = timestamp;
            _summer.AddNewData(timestamp, TNumber.One);
        }

        for (var index = 0; index < OperationsPerBatch; index++)
        {
            _values[index] = TNumber.CreateChecked((index % 7) + 1);
        }

        _seedDataCount = _summer.DataCount;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public TNumber SameBucketUpdates()
    {
        var timestamp = _bucketTimes[0];
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            _summer.AddNewData(timestamp, _values[index]);
        }

        return _summer.Samples[timestamp];
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public TNumber MultipleExistingBucketUpdates()
    {
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            var bucketIndex = index % ExistingBucketCount;
            _summer.AddNewData(_bucketTimes[bucketIndex], _values[index]);
        }

        return _summer.Samples[_bucketTimes[OperationsPerBatch - 1]];
    }

    [GlobalCleanup(Target = nameof(SameBucketUpdates))]
    public void CleanupSameBucketUpdates() => WriteEvidence("same-bucket");

    [GlobalCleanup(Target = nameof(MultipleExistingBucketUpdates))]
    public void CleanupMultipleExistingBucketUpdates() => WriteEvidence("multiple-existing-buckets");

    private void WriteEvidence(string workload)
    {
        var buckets = _summer.Samples;
        TimeSeriesIngressEvidence.ValidateBuckets(buckets, ExistingBucketCount);
        TimeSeriesIngressEvidence.ValidateBatchDataCount(_seedDataCount, _summer.DataCount, OperationsPerBatch);

        var checksum = TimeSeriesIngressEvidence.OffsetBasis;
        foreach (var bucket in buckets)
        {
            checksum = TimeSeriesIngressEvidence.Mix(checksum, unchecked((ulong)bucket.Key.UtcTicks));
            checksum = TimeSeriesIngressEvidence.MixText(
                checksum,
                Convert.ToString(bucket.Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }

        TimeSeriesIngressEvidence.Write(
            workload,
            $"{nameof(NumberTimeSeriesSummer<TNumber>)}<{typeof(TNumber).Name}>",
            ExistingBucketCount,
            _seedDataCount,
            _summer.DataCount,
            buckets.Count,
            checksum);
    }
}
