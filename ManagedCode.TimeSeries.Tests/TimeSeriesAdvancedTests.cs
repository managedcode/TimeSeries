using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Orleans;
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Tests.Assertions;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class TimeSeriesAdvancedTests
{
    [Fact]
    public void IntAccumulator_ParallelAdds_PreserveCountAndOrdering()
    {
        var interval = TimeSpan.FromMilliseconds(1);
        var accumulator = new IntTimeSeriesAccumulator(interval, maxSamplesCount: 10_000);
        var baseline = DateTimeOffset.UnixEpoch;
        const int operations = 20_000;

        Parallel.For(0, operations, i =>
        {
            var timestamp = baseline.AddMilliseconds(i % 32);
            accumulator.AddNewData(timestamp, i);
        });

        accumulator.DataCount.ShouldBe((ulong)operations);
        var totalItems = accumulator.Samples.Values.Sum(queue => queue.Count);
        totalItems.ShouldBe(operations);
        accumulator.Samples.Keys.ShouldBeInAscendingOrder();
    }

    [Fact]
    public void IntAccumulator_TrimRemovesEmptyEdges()
    {
        var accumulator = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 10);

        accumulator.MarkupAllSamples();
        accumulator.AddNewData(DateTimeOffset.UtcNow, 42);

        var countBefore = accumulator.Samples.Count;
        accumulator.TrimStart();
        accumulator.TrimEnd();
        accumulator.Samples.Count.ShouldBeLessThan(countBefore);
        accumulator.Samples.Values.All(queue => !queue.IsEmpty).ShouldBeTrue();
    }

    [Fact]
    public void NumberSummer_ConcurrentUsage_IsConsistent()
    {
        var summer = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(2));
        var start = DateTimeOffset.UnixEpoch;
        const int operations = 15_000;

        Parallel.For(0, operations, i =>
        {
            var timestamp = start.AddMilliseconds(i % 8);
            summer.AddNewData(timestamp, 1);
        });

        summer.Sum().ShouldBe(operations);

        var min = summer.Min();
        min.ShouldNotBeNull();
        min.Value.ShouldBeGreaterThan(0);

        var max = summer.Max();
        max.ShouldNotBeNull();
        max.Value.ShouldBeGreaterThan(0);

        summer.Average().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NumberSummer_MergeRetainsTotals()
    {
        var left = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(5));
        var right = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(5));
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
        {
            left.AddNewData(now.AddMilliseconds(i), 2);
            right.AddNewData(now.AddMilliseconds(i), 3);
        }

        left.Merge(right);
        left.Sum().ShouldBe(50);
        left.DataCount.ShouldBeGreaterThan(0ul);
        left.Sum().ShouldBe(left.Samples.Values.Sum(value => value));
    }

    [Fact]
    public void NumberSummer_ResampleAggregatesValues()
    {
        var start = DateTimeOffset.UnixEpoch;
        var summer = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(1));

        for (var i = 0; i < 8; i++)
        {
            summer.AddNewData(start.AddMilliseconds(i), i + 1);
        }

        summer.Resample(TimeSpan.FromMilliseconds(4), samplesCount: 4);

        summer.SampleInterval.ShouldBe(TimeSpan.FromMilliseconds(4));
        summer.Samples.Count.ShouldBeLessThanOrEqualTo(4);
        summer.Sum().ShouldBe(36);
    }

    [Fact]
    public void NumberGroupSummer_ParallelUpdates_AreAggregated()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(
            sampleInterval: TimeSpan.FromMilliseconds(5),
            samplesCount: 128,
            strategy: Strategy.Sum,
            deleteOverdueSamples: false);

        var keys = Enumerable.Range(0, 16).Select(i => $"key-{i}").ToArray();

        Parallel.ForEach(keys, key =>
        {
            for (var i = 0; i < 1_000; i++)
            {
                group.AddNewData(key, 1);
            }
        });

        group.Sum().ShouldBe(16_000);
        group.Average().ShouldBeGreaterThan(0);
        group.Min().ShouldBeGreaterThan(0);
        group.Max().ShouldBeGreaterThanOrEqualTo(group.Min());
    }

    [Fact]
    public void IntGroupAccumulator_ConcurrentAdds_AreSnapshotSafe()
    {
        var group = new IntGroupTimeSeriesAccumulator(
            sampleInterval: TimeSpan.FromMilliseconds(5),
            maxSamplesCount: 256,
            deleteOverdueSamples: false);

        var keys = Enumerable.Range(0, 8).Select(i => $"group-{i}").ToArray();

        Parallel.ForEach(keys, key =>
        {
            for (var i = 0; i < 500; i++)
            {
                group.AddNewData(key, DateTimeOffset.UnixEpoch.AddMilliseconds(i), i);
            }
        });

        var snapshot = group.Snapshot();
        snapshot.ShouldHaveCount(8);
        snapshot.Aggregate(0UL, (total, pair) => total + pair.Value.DataCount).ShouldBe(8 * 500ul);
    }

    [Fact]
    public void Accumulator_DeleteOverdueSamples_RemovesOldEntries()
    {
        var accumulator = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 5);
        var baseTime = DateTimeOffset.UnixEpoch;

        for (var i = 0; i < 10; i++)
        {
            accumulator.AddNewData(baseTime.AddMilliseconds(i * 10), i);
        }

        accumulator.Samples.Count.ShouldBe(5);

        accumulator.DeleteOverdueSamples();
        accumulator.Samples.Count.ShouldBeLessThanOrEqualTo(5);
        accumulator.Samples.Keys.ShouldBeInAscendingOrder();
    }

    [Fact]
    public void OrleansAccumulatorConverter_RoundTrips()
    {
        var accumulator = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 16);
        for (var i = 0; i < 8; i++)
        {
            accumulator.AddNewData(DateTimeOffset.UnixEpoch.AddSeconds(i), i);
        }

        var converter = new IntTimeSeriesAccumulatorConverter();
        var surrogate = converter.ConvertToSurrogate(accumulator);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.DataCount.ShouldBe(accumulator.DataCount);
        restored.Samples.Count.ShouldBe(accumulator.Samples.Count);
        restored.Samples.Keys.ShouldSequenceEqual(accumulator.Samples.Keys.ToArray());
    }

    [Fact]
    public void OrleansSummerConverter_RoundTrips()
    {
        var summer = new IntTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 64, strategy: Strategy.Sum);
        for (var i = 0; i < 10; i++)
        {
            summer.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(i), i);
        }

        var converter = new IntTimeSeriesSummerConverter();
        var surrogate = converter.ConvertToSurrogate(summer);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(summer.Sum());
        restored.Samples.Count.ShouldBe(summer.Samples.Count);
        restored.Samples.Keys.ShouldSequenceEqual(summer.Samples.Keys.ToArray());
    }

    [Fact]
    public void OrleansGroupAccumulatorConverter_RoundTrips()
    {
        var group = new IntGroupTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 8, deleteOverdueSamples: false);
        group.AddNewData("a", DateTimeOffset.UnixEpoch, 1);
        group.AddNewData("b", DateTimeOffset.UnixEpoch.AddSeconds(1), 2);

        var converter = new IntGroupTimeSeriesAccumulatorConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Snapshot().Count.ShouldBe(2);
        restored.Snapshot().Single(pair => pair.Key == "a").Value.DataCount.ShouldBe(1ul);
        restored.Snapshot().Single(pair => pair.Key == "b").Value.DataCount.ShouldBe(1ul);
    }

    [Fact]
    public void OrleansGroupSummerConverter_RoundTrips()
    {
        var group = new IntGroupNumberTimeSeriesSummer(TimeSpan.FromSeconds(1), samplesCount: 8, strategy: Strategy.Sum, deleteOverdueSamples: false);
        group.AddNewData("a", 1);
        group.AddNewData("b", 2);

        var converter = new IntGroupNumberTimeSeriesSummerConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(3);
        restored.TimeSeries.Count.ShouldBe(2);
    }

    [Fact]
    public void OrleansGenericSummerConverter_RoundTrips()
    {
        var summer = new NumberTimeSeriesSummer<decimal>(TimeSpan.FromSeconds(1), maxSamplesCount: 4, strategy: Strategy.Sum);
        summer.AddNewData(1.5m);
        summer.AddNewData(2.5m);

        var converter = new NumberTimeSeriesSummerConverter<decimal>();
        var surrogate = converter.ConvertToSurrogate(summer);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(4.0m);
        restored.Samples.Count.ShouldBe(summer.Samples.Count);
    }

    [Fact]
    public void OrleansGenericGroupSummerConverter_RoundTrips()
    {
        var group = new NumberGroupTimeSeriesSummer<decimal>(TimeSpan.FromSeconds(1), samplesCount: 4, strategy: Strategy.Sum, deleteOverdueSamples: false);
        group.AddNewData("a", 1.0m);
        group.AddNewData("b", 2.0m);

        var converter = new NumberGroupTimeSeriesSummerConverter<decimal>();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(3.0m);
        restored.TimeSeries.Count.ShouldBe(2);
    }

    [Fact]
    public void OrleansFloatGroupAccumulatorConverter_RoundTrips()
    {
        var group = new FloatGroupTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 4, deleteOverdueSamples: false);
        group.AddNewData("alpha", DateTimeOffset.UnixEpoch, 1.25f);
        group.AddNewData("beta", DateTimeOffset.UnixEpoch.AddSeconds(1), 2.5f);

        var converter = new FloatGroupTimeSeriesAccumulatorConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Snapshot().Count.ShouldBe(2);
        restored.Snapshot().Single(pair => pair.Key == "alpha").Value.DataCount.ShouldBe(1ul);
    }

    [Fact]
    public void OrleansDoubleGroupAccumulatorConverter_RoundTrips()
    {
        var group = new DoubleGroupTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 4, deleteOverdueSamples: false);
        group.AddNewData("alpha", DateTimeOffset.UnixEpoch, 1.25);
        group.AddNewData("beta", DateTimeOffset.UnixEpoch.AddSeconds(1), 2.5);

        var converter = new DoubleGroupTimeSeriesAccumulatorConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Snapshot().Count.ShouldBe(2);
        restored.Snapshot().Single(pair => pair.Key == "beta").Value.DataCount.ShouldBe(1ul);
    }

    [Fact]
    public void OrleansFloatGroupSummerConverter_RoundTrips()
    {
        var group = new FloatGroupNumberTimeSeriesSummer(TimeSpan.FromSeconds(1), samplesCount: 4, strategy: Strategy.Sum, deleteOverdueSamples: false);
        group.AddNewData("alpha", 1.5f);
        group.AddNewData("beta", 2.5f);

        var converter = new FloatGroupNumberTimeSeriesSummerConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(4.0f);
        restored.TimeSeries.Count.ShouldBe(2);
    }

    [Fact]
    public void OrleansDoubleGroupSummerConverter_RoundTrips()
    {
        var group = new DoubleGroupTimeSeriesSummer(TimeSpan.FromSeconds(1), samplesCount: 4, strategy: Strategy.Sum, deleteOverdueSamples: false);
        group.AddNewData("alpha", 1.5);
        group.AddNewData("beta", 2.5);

        var converter = new DoubleGroupTimeSeriesSummerConverter();
        var surrogate = converter.ConvertToSurrogate(group);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().ShouldBe(4.0);
        restored.TimeSeries.Count.ShouldBe(2);
    }
}
