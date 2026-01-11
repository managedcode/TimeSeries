using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Extensions;
using ManagedCode.TimeSeries.Tests.Assertions;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class AccumulatorsTests
{

    [Fact]
    public void IntTimeSeriesAccumulator()
    {
        var count = 1050;
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval);
        var start = DateTimeOffset.UtcNow;
        for (var i = 0; i < count; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.DataCount.ShouldBe(Convert.ToUInt64(count));

        var step = 0;
        foreach (var queue in series.Samples)
        {
            foreach (var item in queue.Value)
            {
                item.ShouldBe(step);
                step++;
            }
        }
    }

    [Fact]
    public void IntTimeSeriesAccumulatorMaxSamplesCount()
    {
        var samplesCount = 105;
        var count = 1050;
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval, samplesCount);
        var start = DateTimeOffset.UtcNow;
        for (var i = 0; i < count; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.DataCount.ShouldBe(Convert.ToUInt64(count)); //because it's total; number of samples
        series.Samples.Count.ShouldBe(samplesCount); //because it's total; number of samples

        var ordered = series.Samples.OrderBy(pair => pair.Key).ToArray();
        ordered.Length.ShouldBe(samplesCount);
        ordered.First().Value.Single().ShouldBe(count - samplesCount);
        ordered.Last().Value.Single().ShouldBe(count - 1);
    }

    [Fact]
    public void GroupAccumulatorSupportsMultipleKeys()
    {
        var group = new IntGroupTimeSeriesAccumulator(TimeSpan.FromMilliseconds(50), maxSamplesCount: 10, deleteOverdueSamples: false);
        var origin = DateTimeOffset.UtcNow;

        group.AddNewData("alpha", origin, 1);
        group.AddNewData("alpha", origin.AddMilliseconds(60), 2);
        group.AddNewData("beta", origin, 5);

        group.TryGet("alpha", out var alphaAccumulator).ShouldBeTrue();
        alphaAccumulator.ShouldNotBeNull();
        alphaAccumulator!.DataCount.ShouldBe(2ul);
        alphaAccumulator.Samples.ShouldHaveCount(2);

        var snapshot = group.Snapshot();
        snapshot.Count.ShouldBe(2);

        group.Remove("beta").ShouldBeTrue();
        group.TryGet("beta", out _).ShouldBeFalse();
    }

    [Fact]
    public void DoubleAccumulatorStoresValues()
    {
        var interval = TimeSpan.FromMilliseconds(25);
        var accumulator = new DoubleTimeSeriesAccumulator(interval, maxSamplesCount: 4);
        var now = DateTimeOffset.UtcNow;

        accumulator.AddNewData(now, 1.5);
        accumulator.AddNewData(now.AddMilliseconds(25), 2.5);

        accumulator.Samples.ShouldHaveCount(2);
        accumulator.Samples.TryGetValue(now.Round(interval), out var firstQueue).ShouldBeTrue();
        firstQueue.ShouldNotBeNull();
        firstQueue!.ShouldContain(1.5);
    }

    [Fact]
    public void DoubleGroupAccumulatorHandlesMultipleStreams()
    {
        var group = new DoubleGroupTimeSeriesAccumulator(TimeSpan.FromMilliseconds(50), maxSamplesCount: 2, deleteOverdueSamples: false);
        var origin = DateTimeOffset.UtcNow;

        group.AddNewData("alpha", origin, 1.0);
        group.AddNewData("alpha", origin.AddMilliseconds(50), 2.0);
        group.AddNewData("beta", origin, 3.0);

        group.TryGet("alpha", out var alpha).ShouldBeTrue();
        alpha.ShouldNotBeNull();
        alpha!.Samples.ShouldHaveCount(2);
        alpha.DataCount.ShouldBe(2ul);

        group.TryGet("beta", out var beta).ShouldBeTrue();
        beta.ShouldNotBeNull();
        beta!.Samples.ShouldHaveCount(1);
    }

    [Fact]
    public void FloatGroupAccumulatorTracksSamples()
    {
        var group = new FloatGroupTimeSeriesAccumulator(TimeSpan.FromMilliseconds(40), maxSamplesCount: 2, deleteOverdueSamples: false);
        var origin = DateTimeOffset.UtcNow;

        group.AddNewData("alpha", origin, 1.0f);
        group.AddNewData("alpha", origin.AddMilliseconds(40), 2.0f);

        group.TryGet("alpha", out var alpha).ShouldBeTrue();
        alpha.ShouldNotBeNull();
        alpha!.Samples.ShouldHaveCount(2);
    }

    [Fact]
    public void TrimRemovesEmptyBoundarySamples()
    {
        var interval = TimeSpan.FromSeconds(1);
        var series = new IntTimeSeriesAccumulator(interval, maxSamplesCount: 3);

        series.MarkupAllSamples();
        series.AddNewData(series.Start, 7);

        series.Trim();

        series.Samples.ShouldHaveCount(1);
        var queue = series.Samples.Single().Value;
        queue.Count.ShouldBe(1);
        queue.ShouldContain(7);
    }

    [Fact]
    public void Accumulator()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval);
        var start = DateTimeOffset.UtcNow;
        for (var i = 0; i < 1000; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.DataCount.ShouldBe(1000ul);

        var step = 0;
        foreach (var queue in series.Samples)
        {
            foreach (var item in queue.Value)
            {
                item.ShouldBe(step);
                step++;
            }
        }
    }

    // [Fact]
    // public async Task AccumulatorByString()
    // {
    //     var rnd = new Random();
    //     var series = new StringTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));
    //
    //     var dt = DateTimeOffset.Now;
    //     series.AddNewData(dt, "1");
    //     series.AddNewData(dt, "1");
    //     series.AddNewData(dt, "2");
    //     series.AddNewData(dt, "3");
    //     series.AddNewData(dt, "3");
    //     series.AddNewData(dt, "2");
    //
    //
    //     dt = dt.AddHours(5);
    //     series.AddNewData(dt, "1");
    //     series.AddNewData(dt, "1");
    //     series.AddNewData(dt, "2");
    //     series.AddNewData(dt, "3");
    //     series.AddNewData(dt, "3");
    //     series.AddNewData(dt, "2");
    //
    //     series.DataCount.ShouldBe(12);
    //     series.Samples.First().Value.Count.ShouldBe(3);
    //     series.Samples.Last().Value.Count.ShouldBe(3);
    // }

    [Fact]
    public void AccumulatorLimit()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval, 10);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 1000; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.Samples.Count.ShouldBe(10);
    }

    [Fact]
    public void IsFull()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval, 10);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.IsFull.ShouldBeTrue();
    }

    [Fact]
    public void IsFull_UnboundedSeriesRemainsFalse()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 50; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        series.IsFull.ShouldBeFalse();
    }

    [Fact]
    public void IsEmpty()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
        series.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void AccumulatorMerge()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        Func<IntTimeSeriesAccumulator> fillFunc = () =>
        {
            var series = new IntTimeSeriesAccumulator(interval, 10);
            var start = DateTimeOffset.UtcNow;
            for (var i = 0; i < 1000; i++)
            {
                series.AddNewData(start.AddTicks(interval.Ticks * i), i);
            }

            return series;
        };

        var seriesA = fillFunc();
        var seriesB = fillFunc();

        seriesA.Samples.Count.ShouldBe(10);
        seriesB.Samples.Count.ShouldBe(10);

        seriesA.Merge(seriesB);

        seriesA.Samples.Count.ShouldBe(10);

        var seriesList = new List<IntTimeSeriesAccumulator>();
        seriesList.Add(fillFunc());
        seriesList.Add(fillFunc());
        seriesList.Add(fillFunc());
        seriesList.Add(new IntTimeSeriesAccumulator(interval, 10));

        IntTimeSeriesAccumulator? merged = null;
        foreach (var item in seriesList)
        {
            if (merged is null)
            {
                merged = item;
            }
            else
            {
                merged.Merge(item);
            }
        }

        merged.ShouldNotBeNull();
        merged!.Samples.Count.ShouldBe(10);
    }

    [Fact]
    public void Resample()
    {
        var interval = TimeSpan.FromMilliseconds(2);
        var seriesFeature = new IntTimeSeriesAccumulator(interval, 100);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 100; i++)
        {
            seriesFeature.AddNewData(start.AddMilliseconds(i * interval.TotalMilliseconds), i);
        }

        seriesFeature.Resample(TimeSpan.FromMilliseconds(4), 100);
        seriesFeature.SampleInterval.ShouldBe(TimeSpan.FromMilliseconds(4));
        seriesFeature.Samples.ShouldNotBeEmpty();
    }

    [Fact]
    public void MarkupAllSamples()
    {
        var seriesFeature = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), 100);
        seriesFeature.MarkupAllSamples(MarkupDirection.Future);
        seriesFeature.AddNewData(1);
        (seriesFeature.Samples.Keys.Max() - seriesFeature.Samples.Keys.Min()).TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(990);
        (seriesFeature.Samples.Keys.Max() - seriesFeature.Samples.Keys.Min()).TotalMilliseconds.ShouldBeLessThanOrEqualTo(1000);
        var seriesFeatureOrdered = seriesFeature.Samples.OrderBy(o => o.Key).Take(10);
        seriesFeatureOrdered.Any(a => a.Value.Count == 1).ShouldBeTrue();

        var seriesPast = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10));
        seriesPast.MarkupAllSamples();
        seriesPast.AddNewData(1);
        (seriesPast.Samples.Keys.Max() - seriesPast.Samples.Keys.Min()).TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(990);
        (seriesPast.Samples.Keys.Max() - seriesPast.Samples.Keys.Min()).TotalMilliseconds.ShouldBeLessThanOrEqualTo(1000);
        var seriesPastOrdered = seriesPast.Samples.OrderBy(o => o.Key).TakeLast(10);
        seriesPastOrdered.Any(a => a.Value.Count == 1).ShouldBeTrue();

        var seriesMiddle = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), 100);
        seriesMiddle.MarkupAllSamples(MarkupDirection.Middle);
        seriesMiddle.AddNewData(1);
        (seriesMiddle.Samples.Keys.Max() - seriesMiddle.Samples.Keys.Min()).TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(990);
        (seriesMiddle.Samples.Keys.Max() - seriesMiddle.Samples.Keys.Min()).TotalMilliseconds.ShouldBeLessThanOrEqualTo(1000);
        var seriesMiddleOrdered = seriesMiddle.Samples.OrderBy(o => o.Key).Skip(45).Take(10);
        seriesMiddleOrdered.Any(a => a.Value.Count == 1).ShouldBeTrue();
    }
}
