using FluentAssertions;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
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

        series.DataCount.Should().Be(1000);

        var step = 0;
        foreach (var queue in series.Samples)
        {
            foreach (var item in queue.Value)
            {
                item.Should().Be(step);
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
    //     series.DataCount.Should().Be(12);
    //     series.Samples.First().Value.Count.Should().Be(3);
    //     series.Samples.Last().Value.Count.Should().Be(3);
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

        series.Samples.Count.Should().Be(10);
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

        series.IsFull.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
        series.IsEmpty.Should().BeTrue();
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

        seriesA.Result.Samples.Count.Should().Be(10);
        seriesB.Result.Samples.Count.Should().Be(10);

        seriesA.Result.Merge(seriesB.Result);

        seriesA.Result.Samples.Count.Should().Be(10);

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

        onlineExpertsPerHourTimeSeries.Samples.Count.Should().Be(10);
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

        series.DataCount.Should().Be(150);
        series.Samples.First().Value.Should().Be(50);
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

        series.DataCount.Should().Be((ulong) count);
    }

    [Fact]
    public void NumberTimeSeriesSummerSupportsLong()
    {
        var baseTime = DateTimeOffset.UtcNow;
        var series = new NumberTimeSeriesSummer<long>(TimeSpan.FromSeconds(1));

        series.AddNewData(baseTime, 10);
        series.AddNewData(baseTime.AddSeconds(1), 15);

        series.Sum().Should().Be(25);
        series.Min().Should().Be(10);
        series.Max().Should().Be(15);
        series.Average().Should().Be(12);
    }

    [Fact]
    public void NumberGroupTimeSeriesSummerAggregatesDecimal()
    {
        var group = new NumberGroupTimeSeriesSummer<decimal>(TimeSpan.FromSeconds(1), samplesCount: 32, strategy: Strategy.Sum, deleteOverdueSamples: false);

        group.AddNewData("a", 1.5m);
        group.AddNewData("a", 2.0m);
        group.AddNewData("b", 3.0m);

        group.Sum().Should().Be(6.5m);
        group.Min().Should().Be(3.0m);
        group.Max().Should().Be(3.5m);
        group.Average().Should().Be(3.25m);
    }

    [Fact]
    public void NumberGroupTimeSeriesSummerHandlesEmptyState()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(TimeSpan.FromSeconds(1), deleteOverdueSamples: false);

        group.Sum().Should().Be(0);
        group.Average().Should().Be(0);
        group.Min().Should().Be(0);
        group.Max().Should().Be(0);
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

        summer.SampleInterval.Should().Be(TimeSpan.FromSeconds(2));
        summer.Samples.Should().HaveCount(2);
        summer.Samples[start].Should().Be(3);
        summer.Samples[start.AddSeconds(2)].Should().Be(3);
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
    //     series.TimeSeries.Count.Should().BeGreaterThan(0);
    //     await Task.Delay(interval * 102);
    //
    //     series.TimeSeries.Count.Should().Be(0);
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
    //     series.TimeSeries.Count.Should().Be(1);
    //     series.TimeSeries.Values.SingleOrDefault().Samples.SingleOrDefault().Value.Should().Be(99);
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

        seriesA.Result.DataCount.Should().Be(100);
        seriesB.Result.DataCount.Should().Be(100);

        seriesA.Result.Merge(seriesB.Result);

        seriesA.Result.DataCount.Should().Be(200);

        seriesA.Result.Samples.Select(s => s.Value).Sum().Should().Be(200);
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
        (seriesFeature.Samples.Keys.Max() - seriesFeature.Samples.Keys.Min()).TotalMilliseconds.Should().BeGreaterThanOrEqualTo(990);
        (seriesFeature.Samples.Keys.Max() - seriesFeature.Samples.Keys.Min()).TotalMilliseconds.Should().BeLessThanOrEqualTo(1000);
        var seriesFeatureOrdered = seriesFeature.Samples.OrderBy(o => o.Key).Take(10);
        seriesFeatureOrdered.Any(a => a.Value.Count == 1).Should().BeTrue();

        var seriesPast = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10));
        seriesPast.MarkupAllSamples();
        seriesPast.AddNewData(1);
        (seriesPast.Samples.Keys.Max() - seriesPast.Samples.Keys.Min()).TotalMilliseconds.Should().BeGreaterThanOrEqualTo(990);
        (seriesPast.Samples.Keys.Max() - seriesPast.Samples.Keys.Min()).TotalMilliseconds.Should().BeLessThanOrEqualTo(1000);
        var seriesPastOrdered = seriesPast.Samples.OrderBy(o => o.Key).TakeLast(10);
        seriesPastOrdered.Any(a => a.Value.Count == 1).Should().BeTrue();

        var seriesMiddle = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), 100);
        seriesMiddle.MarkupAllSamples(MarkupDirection.Middle);
        seriesMiddle.AddNewData(1);
        (seriesMiddle.Samples.Keys.Max() - seriesMiddle.Samples.Keys.Min()).TotalMilliseconds.Should().BeGreaterThanOrEqualTo(990);
        (seriesMiddle.Samples.Keys.Max() - seriesMiddle.Samples.Keys.Min()).TotalMilliseconds.Should().BeLessThanOrEqualTo(1000);
        var seriesMiddleOrdered = seriesMiddle.Samples.OrderBy(o => o.Key).Skip(45).Take(10);
        seriesMiddleOrdered.Any(a => a.Value.Count == 1).Should().BeTrue();
    }
}
