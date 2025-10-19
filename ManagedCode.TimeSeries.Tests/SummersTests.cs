using Shouldly;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Extensions;
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Tests.Assertions;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class SummersTests
{
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
    public void IntTimeSeriesSummerIncrementDecrement()
    {
        var series = new IntTimeSeriesSummer(TimeSpan.FromMinutes(1), 10);
        for (var i = 0; i < 100; i++)
        {
            series.Increment();
        }

        for (var i = 0; i < 50; i++)
        {
            series.Decrement();
        }

        series.DataCount.ShouldBe(150ul);
        series.Samples.First().Value.ShouldBe(50);
    }

    [Fact]
    public async Task Summer()
    {
        var series = new IntTimeSeriesSummer(TimeSpan.FromSeconds(0.1));
        var count = 0;
        for (var i = 0; i < 100; i++)
        {
            await Task.Delay(new Random().Next(10, 50));
            series.AddNewData(i);
            count++;
        }

        series.DataCount.ShouldBe((ulong) count);
    }

    [Fact]
    public void NumberTimeSeriesSummerSupportsLong()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var series = new NumberTimeSeriesSummer<long>(TimeSpan.FromSeconds(1));

        series.AddNewData(baseTime, 10);
        series.AddNewData(baseTime.AddSeconds(1), 15);

        series.Sum().ShouldBe(25);
        series.Min().ShouldBe(10);
        series.Max().ShouldBe(15);
        series.Average().ShouldBe(12);
    }

    [Fact]
    public void NumberGroupTimeSeriesSummerAggregatesDecimal()
    {
        var group = new NumberGroupTimeSeriesSummer<decimal>(TimeSpan.FromSeconds(1), samplesCount: 32, strategy: Strategy.Sum, deleteOverdueSamples: false);

        group.AddNewData("a", 1.5m);
        group.AddNewData("a", 2.0m);
        group.AddNewData("b", 3.0m);

        group.Sum().ShouldBe(6.5m);
        group.Min().ShouldBe(3.0m);
        group.Max().ShouldBe(3.5m);
        group.Average().ShouldBe(3.25m);
    }

    [Fact]
    public void NumberGroupTimeSeriesSummerHandlesEmptyState()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(TimeSpan.FromSeconds(1), deleteOverdueSamples: false);

        group.Sum().ShouldBe(0);
        group.Average().ShouldBe(0);
        group.Min().ShouldBe(0);
        group.Max().ShouldBe(0);
    }

    [Fact]
    public void IntGroupNumberTimeSeriesSummerAggregatesValues()
    {
        var group = new IntGroupNumberTimeSeriesSummer(TimeSpan.FromMilliseconds(5), samplesCount: 8, strategy: Strategy.Sum, deleteOverdueSamples: false);

        group.AddNewData("alpha", 1);
        group.AddNewData("alpha", 2);
        group.AddNewData("beta", 3);

        group.Sum().ShouldBe(6);
        group.Max().ShouldBe(3);
        group.Min().ShouldBe(3);
    }

    [Fact]
    public void DoubleGroupTimeSeriesSummerAggregatesValues()
    {
        var group = new DoubleGroupTimeSeriesSummer(TimeSpan.FromSeconds(1), samplesCount: 8, strategy: Strategy.Sum, deleteOverdueSamples: false);

        group.AddNewData("sensor", 1.5);
        group.AddNewData("sensor", 2.5);
        group.AddNewData("backup", 2.0);

        group.Sum().ShouldBe(6.0);
        group.Max().ShouldBe(4.0);
        group.Min().ShouldBe(2.0);
    }

    [Fact]
    public void FloatGroupNumberTimeSeriesSummerAggregatesValues()
    {
        var group = new FloatGroupNumberTimeSeriesSummer(TimeSpan.FromMilliseconds(100), samplesCount: 4, strategy: Strategy.Sum, deleteOverdueSamples: false);

        group.AddNewData("alpha", 1.0f);
        group.AddNewData("alpha", 2.0f);
        group.AddNewData("beta", 3.5f);

        group.Sum().ShouldBe(6.5f);
        group.Max().ShouldBe(3.5f);
        group.Min().ShouldBe(3.0f);
    }

    [Fact]
    public void NumberTimeSeriesSummerResampleAggregatesBuckets()
    {
        var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var summer = new NumberTimeSeriesSummer<int>(TimeSpan.FromSeconds(1), maxSamplesCount: 10);

        summer.AddNewData(start, 1);
        summer.AddNewData(start.AddSeconds(1), 2);
        summer.AddNewData(start.AddSeconds(2), 3);

        summer.Resample(TimeSpan.FromSeconds(2), 10);

        summer.SampleInterval.ShouldBe(TimeSpan.FromSeconds(2));
        summer.Samples.ShouldHaveCount(2);
        summer.Samples[start].ShouldBe(3);
        summer.Samples[start.AddSeconds(2)].ShouldBe(3);
    }

    [Fact]
    public void NumberTimeSeriesSummerResamplePreservesTotals()
    {
        var interval = TimeSpan.FromMilliseconds(20);
        var summer = new NumberTimeSeriesSummer<int>(interval, maxSamplesCount: 8);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 6; i++)
        {
            summer.AddNewData(start.AddMilliseconds(i * interval.TotalMilliseconds), 2);
        }

        var countBefore = summer.DataCount;
        var lastBefore = summer.LastDate;

        summer.Resample(TimeSpan.FromMilliseconds(40), samplesCount: 4);

        summer.DataCount.ShouldBe(countBefore);
        summer.LastDate.ShouldBe(lastBefore.Round(TimeSpan.FromMilliseconds(40)));
        summer.Sum().ShouldBe(12);
    }

    [Fact]
    public void DoubleTimeSeriesSummerConstructorsRespectStrategy()
    {
        var explicitStrategy = new DoubleTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 3, strategy: Strategy.Max);
        explicitStrategy.Strategy.ShouldBe(Strategy.Max);

        var withCount = new DoubleTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 2);
        withCount.Strategy.ShouldBe(Strategy.Sum);

        var defaultCount = new DoubleTimeSeriesSummer(TimeSpan.FromMilliseconds(5));
        defaultCount.Strategy.ShouldBe(Strategy.Sum);
    }

    [Fact]
    public void FloatTimeSeriesSummerAggregatesValues()
    {
        var summer = new FloatTimeSeriesSummer(TimeSpan.FromMilliseconds(30), maxSamplesCount: 4);
        var now = DateTimeOffset.UtcNow;

        summer.AddNewData(now, 1.5f);
        summer.AddNewData(now.AddMilliseconds(30), 2.5f);

        summer.Sum().ShouldBe(4.0f);
        summer.Min().ShouldNotBeNull();
        summer.Max().ShouldNotBeNull();
    }

    // [Fact]
    // public async Task SummerGroup()
    // {
    //     var interval = TimeSpan.FromSeconds(0.1);
    //     var series = new IntGroupTimeSeriesSummer(interval, 100, Strategy.Sum, true);
    //     var count = 0;
    //     for (var i = 0; i < 100; i++)
    //     {
    //         await Task.Delay(new Random().Next(10, 50));
    //         series.AddNewData((i % 10).ToString(), i);
    //         count++;
    //     }
    //
    //     series.TimeSeries.Count.ShouldBeGreaterThan(0);
    //     await Task.Delay(interval * 102);
    //
    //     series.TimeSeries.Count.ShouldBe(0);
    // }
    //
    // [Fact]
    // public async Task SummerGroupMax()
    // {
    //     var interval = TimeSpan.FromSeconds(5);
    //     var series = new IntGroupTimeSeriesSummer(interval, 100, Strategy.Max, true);
    //     var count = 0;
    //     for (var i = 0; i < 100; i++)
    //     {
    //         series.AddNewData("host", i);
    //         count++;
    //     }
    //
    //     series.TimeSeries.Count.ShouldBe(1);
    //     series.TimeSeries.Values.SingleOrDefault().Samples.SingleOrDefault().Value.ShouldBe(99);
    // }

    [Fact]
    public async Task SummerMerge()
    {
        Func<Task<IntTimeSeriesSummer>> FillFunc = async () =>
        {
            var series = new IntTimeSeriesSummer(TimeSpan.FromSeconds(0.1));

            for (var i = 0; i < 100; i++)
            {
                await Task.Delay(new Random().Next(10, 50));
                series.AddNewData(1);
            }

            return series;
        };

        var seriesA = FillFunc();
        var seriesB = FillFunc();

        await Task.WhenAll(seriesA, seriesB);

        seriesA.Result.DataCount.ShouldBe(100ul);
        seriesB.Result.DataCount.ShouldBe(100ul);

        seriesA.Result.Merge(seriesB.Result);

        seriesA.Result.DataCount.ShouldBe(200ul);

        seriesA.Result.Samples.Select(s => s.Value).Sum().ShouldBe(200);
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
    public async Task ResampleSummer()
    {
        var seriesFeature = new IntTimeSeriesSummer(TimeSpan.FromMilliseconds(2), 100);

        for (var i = 0; i < 100; i++)
        {
            seriesFeature.AddNewData(i);

            await Task.Delay(1);
        }

        seriesFeature.Resample(TimeSpan.FromMilliseconds(4), 100);
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
