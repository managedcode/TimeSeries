using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Orleans;
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

        accumulator.DataCount.Should().Be((ulong)operations);
        var totalItems = accumulator.Samples.Values.Sum(queue => queue.Count);
        totalItems.Should().Be(operations);
        accumulator.Samples.Keys.Should().BeInAscendingOrder();
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
        accumulator.Samples.Count.Should().BeLessThan(countBefore);
        accumulator.Samples.Values.All(queue => queue.Count > 0).Should().BeTrue();
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

        summer.Sum().Should().Be(operations);
        summer.Min().Should().BeGreaterThan(0);
        summer.Max().Should().NotBeNull();
        summer.Max()!.Value.Should().BeGreaterThan(0);
        summer.Average().Should().BeGreaterThan(0);
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
        left.Sum().Should().Be(50);
        left.DataCount.Should().BeGreaterThan(0);
        left.Sum().Should().Be(left.Samples.Values.Sum(value => value));
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

        summer.SampleInterval.Should().Be(TimeSpan.FromMilliseconds(4));
        summer.Samples.Count.Should().BeLessThanOrEqualTo(4);
        summer.Sum().Should().Be(36);
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

        group.Sum().Should().Be(16_000);
        group.Average().Should().BeGreaterThan(0);
        group.Min().Should().BeGreaterThan(0);
        group.Max().Should().BeGreaterThanOrEqualTo(group.Min());
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
        snapshot.Should().HaveCount(8);
        snapshot.Aggregate(0UL, (total, pair) => total + pair.Value.DataCount).Should().Be(8 * 500ul);
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

        accumulator.Samples.Count.Should().Be(5);

        accumulator.DeleteOverdueSamples();
        accumulator.Samples.Count.Should().BeLessThanOrEqualTo(5);
        accumulator.Samples.Keys.Should().BeInAscendingOrder();
    }

    [Fact]
    public void OrleansAccumulatorConverter_RoundTrips()
    {
        var accumulator = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 16);
        for (var i = 0; i < 8; i++)
        {
            accumulator.AddNewData(DateTimeOffset.UnixEpoch.AddSeconds(i), i);
        }

        var converter = new IntTimeSeriesAccumulatorConverter<int>();
        var surrogate = converter.ConvertToSurrogate(accumulator);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.DataCount.Should().Be(accumulator.DataCount);
        restored.Samples.Count.Should().Be(accumulator.Samples.Count);
        restored.Samples.Keys.Should().Equal(accumulator.Samples.Keys.ToArray());
    }

    [Fact]
    public void OrleansSummerConverter_RoundTrips()
    {
        var summer = new IntTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 64, strategy: Strategy.Sum);
        for (var i = 0; i < 10; i++)
        {
            summer.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(i), i);
        }

        var converter = new IntTimeSeriesSummerConverter<int>();
        var surrogate = converter.ConvertToSurrogate(summer);
        var restored = converter.ConvertFromSurrogate(surrogate);

        restored.Sum().Should().Be(summer.Sum());
        restored.Samples.Count.Should().Be(summer.Samples.Count);
        restored.Samples.Keys.Should().Equal(summer.Samples.Keys.ToArray());
    }
}
