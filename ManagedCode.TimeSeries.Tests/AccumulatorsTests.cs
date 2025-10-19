using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Extensions;
using ManagedCode.TimeSeries.Tests.Assertions;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class AccumulatorsTests
{
    
    [Fact]
    public async Task IntTimeSeriesAccumulator()
    {
        int count = 1050;
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));
        for (int i = 0; i < count; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
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
    
    [Fact(Skip = "Need fix")]
    public async Task IntTimeSeriesAccumulatorMaxSamplesCount()
    {
        int samplesCount = 105;
        int count = 1050;
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(0.1), samplesCount);
        for (int i = 0; i < count; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
        }

        series.DataCount.ShouldBe(Convert.ToUInt64(count)); //because it's total; number of samples
        series.Samples.Count.ShouldBe(samplesCount); //because it's total; number of samples

        var step = count - samplesCount - 1;
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
    public async Task Accumulator()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));
        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
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
    public async Task AccumulatorLimit()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);

        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
        }

        series.Samples.Count.ShouldBe(10);
    }

    [Fact]
    public async Task IsFull()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);

        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));

            if (series.IsFull)
            {
                break;
            }

            series.AddNewData(i);
        }

        series.IsFull.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
        series.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task AccumulatorMerge()
    {
        Func<Task<IntTimeSeriesAccumulator>> FillFunc = async () =>
        {
            var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
            for (var i = 0; i < 1000; i++)
            {
                await Task.Delay(new Random().Next(1, 5));
                series.AddNewData(i);
            }

            return series;
        };

        var seriesA = FillFunc();
        var seriesB = FillFunc();

        await Task.WhenAll(seriesA, seriesB);

        seriesA.Result.Samples.Count.ShouldBe(10);
        seriesB.Result.Samples.Count.ShouldBe(10);

        seriesA.Result.Merge(seriesB.Result);

        seriesA.Result.Samples.Count.ShouldBe(10);

        var seriesList = new List<IntTimeSeriesAccumulator>();
        seriesList.Add(await FillFunc());
        seriesList.Add(await FillFunc());
        seriesList.Add(await FillFunc());
        seriesList.Add(new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10));

        IntTimeSeriesAccumulator onlineExpertsPerHourTimeSeries = null;
        foreach (var item in seriesList.ToArray())
        {
            if (onlineExpertsPerHourTimeSeries == null)
            {
                onlineExpertsPerHourTimeSeries = item;
            }
            else
            {
                onlineExpertsPerHourTimeSeries.Merge(item);
            }
        }

        onlineExpertsPerHourTimeSeries.Samples.Count.ShouldBe(10);
    }

  
    [Fact]
    public async Task Resample()
    {
        var seriesFeature = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(2), 100);

        for (var i = 0; i < 100; i++)
        {
            seriesFeature.AddNewData(i);

            await Task.Delay(1);
        }

        seriesFeature.Resample(TimeSpan.FromMilliseconds(4), 100);
        var sad = seriesFeature;
    }

   

    [Fact]
    public void MarkupAllSamples()
    {
        var seriesFeature = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), 100);
        seriesFeature.MarkupAllSamples(MarkupDirection.Feature);
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
