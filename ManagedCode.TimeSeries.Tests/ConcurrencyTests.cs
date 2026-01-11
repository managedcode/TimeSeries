using System.Collections.Concurrent;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task Accumulator_ConcurrentWritesAndReads_AreSafe()
    {
        var interval = TimeSpan.FromMilliseconds(1);
        var series = new IntTimeSeriesAccumulator(interval);
        var baseTime = DateTimeOffset.UtcNow;
        const int writerCount = 8;
        const int perWriter = 1000;

        var errors = new ConcurrentQueue<Exception>();
        using var cts = new CancellationTokenSource();

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    _ = series.Samples.Count;
                    foreach (var sample in series.Samples)
                    {
                        _ = sample.Value.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        }));

        var writers = Enumerable.Range(0, writerCount).Select(writerId => Task.Run(() =>
        {
            var offset = writerId * perWriter;
            for (var i = 0; i < perWriter; i++)
            {
                series.AddNewData(baseTime.AddTicks(interval.Ticks * (offset + i)), 1);
            }
        }));

        await Task.WhenAll(writers);
        cts.Cancel();
        await Task.WhenAll(readers);

        errors.ShouldBeEmpty();
        series.DataCount.ShouldBe((ulong)(writerCount * perWriter));
        series.Samples.Count.ShouldBe(writerCount * perWriter);
    }

    [Fact]
    public async Task Summer_ConcurrentWritesAndReads_AreSafe()
    {
        var interval = TimeSpan.FromMilliseconds(1);
        var series = new IntTimeSeriesSummer(interval);
        var bucketTime = DateTimeOffset.UtcNow;
        const int writerCount = 8;
        const int perWriter = 2000;

        var errors = new ConcurrentQueue<Exception>();
        using var cts = new CancellationTokenSource();

        var readers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    series.Sum();
                    series.Min();
                    series.Max();
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        }));

        var writers = Enumerable.Range(0, writerCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perWriter; i++)
            {
                series.AddNewData(bucketTime, 1);
            }
        }));

        await Task.WhenAll(writers);
        cts.Cancel();
        await Task.WhenAll(readers);

        errors.ShouldBeEmpty();
        series.DataCount.ShouldBe((ulong)(writerCount * perWriter));
        series.Samples.Count.ShouldBe(1);
        series.Sum().ShouldBe(writerCount * perWriter);
    }

    [Fact]
    public async Task GroupAccumulator_ConcurrentWritesAndReads_AreSafe()
    {
        var interval = TimeSpan.FromMilliseconds(1);
        var group = new IntGroupTimeSeriesAccumulator(interval, maxSamplesCount: 0, deleteOverdueSamples: false);
        var baseTime = DateTimeOffset.UtcNow;
        var keys = new[] { "a", "b", "c", "d" };
        const int writerCount = 4;
        const int perWriter = 1000;

        var errors = new ConcurrentQueue<Exception>();
        using var cts = new CancellationTokenSource();

        var readers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    var snapshot = group.Snapshot();
                    foreach (var entry in snapshot)
                    {
                        entry.Value.Samples.Count.ShouldBeGreaterThanOrEqualTo(0);
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        }));

        var writers = Enumerable.Range(0, writerCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perWriter; i++)
            {
                var key = keys[i % keys.Length];
                group.AddNewData(key, baseTime.AddTicks(interval.Ticks * i), 1);
            }
        }));

        await Task.WhenAll(writers);
        cts.Cancel();
        await Task.WhenAll(readers);

        errors.ShouldBeEmpty();

        var expectedPerKey = writerCount * (perWriter / keys.Length);
        var snapshot = group.Snapshot();
        snapshot.Count.ShouldBe(keys.Length);
        foreach (var key in keys)
        {
            snapshot.Single(pair => pair.Key == key).Value.DataCount.ShouldBe((ulong)expectedPerKey);
        }
    }

    [Fact]
    public async Task GroupSummer_ConcurrentWritesAndReads_AreSafe()
    {
        var interval = TimeSpan.FromMilliseconds(1);
        var group = new IntGroupNumberTimeSeriesSummer(interval, samplesCount: 0, Strategy.Sum, deleteOverdueSamples: false);
        var keys = new[] { "a", "b", "c", "d" };
        const int writerCount = 4;
        const int perWriter = 1000;

        var errors = new ConcurrentQueue<Exception>();
        using var cts = new CancellationTokenSource();

        var readers = Enumerable.Range(0, 2).Select(_ => Task.Run(() =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    _ = group.Sum();
                    _ = group.Min();
                    _ = group.Max();
                    _ = group.Average();
                }
            }
            catch (Exception ex)
            {
                errors.Enqueue(ex);
            }
        }));

        var writers = Enumerable.Range(0, writerCount).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < perWriter; i++)
            {
                var key = keys[i % keys.Length];
                group.AddNewData(key, 1);
            }
        }));

        await Task.WhenAll(writers);
        cts.Cancel();
        await Task.WhenAll(readers);

        errors.ShouldBeEmpty();

        var expectedPerKey = writerCount * (perWriter / keys.Length);
        group.TimeSeries.Count.ShouldBe(keys.Length);
        foreach (var key in keys)
        {
            group.TimeSeries[key].DataCount.ShouldBe((ulong)expectedPerKey);
        }
    }
}
