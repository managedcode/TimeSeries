using BenchmarkDotNet.Attributes;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;

namespace ManagedCode.TimeSeries.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class TimeSeriesIngressCapacityBenchmark
{
    private const int OperationsPerBatch = 64;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(1);
    private static readonly DateTimeOffset FirstBucket = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private IntTimeSeriesAccumulator _sequentialAccumulator = null!;
    private IntTimeSeriesSummer _sequentialSummer = null!;
    private IntTimeSeriesAccumulator _lateAccumulator = null!;
    private IntTimeSeriesSummer _lateSummer = null!;
    private long _nextSequentialIndex;
    private long _nextLateIndex;
    private ulong _seedDataCount;

    [Params(64, 4096)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _sequentialAccumulator = new IntTimeSeriesAccumulator(SampleInterval, Capacity);
        _sequentialSummer = new IntTimeSeriesSummer(SampleInterval, Capacity, Strategy.Sum);
        _lateAccumulator = new IntTimeSeriesAccumulator(SampleInterval, Capacity);
        _lateSummer = new IntTimeSeriesSummer(SampleInterval, Capacity, Strategy.Sum);
        for (var index = 0; index < Capacity; index++)
        {
            var timestamp = FirstBucket.AddTicks(index * SampleInterval.Ticks);
            _sequentialAccumulator.AddNewData(timestamp, index);
            _sequentialSummer.AddNewData(timestamp, index);
            _lateAccumulator.AddNewData(timestamp, index);
            _lateSummer.AddNewData(timestamp, index);
        }

        _nextSequentialIndex = 0;
        _nextLateIndex = 0;
        _seedDataCount = (ulong)Capacity;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public ulong SequentialCappedAccumulatorIngress()
    {
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            var timestamp = FirstBucket.AddTicks((Capacity + _nextSequentialIndex++) * SampleInterval.Ticks);
            _sequentialAccumulator.AddNewData(timestamp, index);
        }

        return _sequentialAccumulator.DataCount + (ulong)_sequentialAccumulator.Samples.Count;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public int SequentialCappedSummerIngress()
    {
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            var timestamp = FirstBucket.AddTicks((Capacity + _nextSequentialIndex++) * SampleInterval.Ticks);
            _sequentialSummer.AddNewData(timestamp, index);
        }

        var lastTimestamp = FirstBucket.AddTicks((Capacity + _nextSequentialIndex - 1) * SampleInterval.Ticks);
        return _sequentialSummer.Samples[lastTimestamp] + (int)(_sequentialSummer.DataCount % int.MaxValue);
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public ulong LateCappedAccumulatorIngress()
    {
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            var timestamp = FirstBucket.AddTicks((Capacity - 1L - _nextLateIndex++) * SampleInterval.Ticks);
            _lateAccumulator.AddNewData(timestamp, index);
        }

        return _lateAccumulator.DataCount + (ulong)_lateAccumulator.Samples.Count;
    }

    [Benchmark(OperationsPerInvoke = OperationsPerBatch)]
    public int LateCappedSummerIngress()
    {
        for (var index = 0; index < OperationsPerBatch; index++)
        {
            var timestamp = FirstBucket.AddTicks((Capacity - 1L - _nextLateIndex++) * SampleInterval.Ticks);
            _lateSummer.AddNewData(timestamp, index);
        }

        var latestSeededTimestamp = FirstBucket.AddTicks((Capacity - 1L) * SampleInterval.Ticks);
        return _lateSummer.Samples[latestSeededTimestamp] + (int)(_lateSummer.DataCount % int.MaxValue);
    }

    [GlobalCleanup(Target = nameof(SequentialCappedAccumulatorIngress))]
    public void CleanupSequentialCappedAccumulatorIngress() => WriteAccumulatorEvidence(
        "sequential-capped",
        nameof(IntTimeSeriesAccumulator),
        _sequentialAccumulator.Samples,
        _sequentialAccumulator.DataCount);

    [GlobalCleanup(Target = nameof(SequentialCappedSummerIngress))]
    public void CleanupSequentialCappedSummerIngress() => WriteSummerEvidence(
        "sequential-capped",
        nameof(IntTimeSeriesSummer),
        _sequentialSummer.Samples,
        _sequentialSummer.DataCount);

    [GlobalCleanup(Target = nameof(LateCappedAccumulatorIngress))]
    public void CleanupLateCappedAccumulatorIngress() => WriteAccumulatorEvidence(
        "late-capped",
        nameof(IntTimeSeriesAccumulator),
        _lateAccumulator.Samples,
        _lateAccumulator.DataCount);

    [GlobalCleanup(Target = nameof(LateCappedSummerIngress))]
    public void CleanupLateCappedSummerIngress() => WriteSummerEvidence(
        "late-capped",
        nameof(IntTimeSeriesSummer),
        _lateSummer.Samples,
        _lateSummer.DataCount);

    private void WriteAccumulatorEvidence(
        string workload,
        string seriesType,
        IReadOnlyDictionary<DateTimeOffset, System.Collections.Concurrent.ConcurrentQueue<int>> buckets,
        ulong dataCount)
    {
        TimeSeriesIngressEvidence.ValidateBuckets(buckets, Capacity);
        TimeSeriesIngressEvidence.ValidateBatchDataCount(_seedDataCount, dataCount, OperationsPerBatch);

        var checksum = TimeSeriesIngressEvidence.OffsetBasis;
        foreach (var bucket in buckets)
        {
            checksum = TimeSeriesIngressEvidence.Mix(checksum, unchecked((ulong)bucket.Key.UtcTicks));
            checksum = TimeSeriesIngressEvidence.Mix(checksum, (ulong)bucket.Value.Count);
        }

        TimeSeriesIngressEvidence.Write(workload, seriesType, Capacity, _seedDataCount, dataCount, buckets.Count, checksum);
    }

    private void WriteSummerEvidence(
        string workload,
        string seriesType,
        IReadOnlyDictionary<DateTimeOffset, int> buckets,
        ulong dataCount)
    {
        TimeSeriesIngressEvidence.ValidateBuckets(buckets, Capacity);
        TimeSeriesIngressEvidence.ValidateBatchDataCount(_seedDataCount, dataCount, OperationsPerBatch);

        var checksum = TimeSeriesIngressEvidence.OffsetBasis;
        foreach (var bucket in buckets)
        {
            checksum = TimeSeriesIngressEvidence.Mix(checksum, unchecked((ulong)bucket.Key.UtcTicks));
            checksum = TimeSeriesIngressEvidence.Mix(checksum, unchecked((ulong)bucket.Value));
        }

        TimeSeriesIngressEvidence.Write(workload, seriesType, Capacity, _seedDataCount, dataCount, buckets.Count, checksum);
    }
}
