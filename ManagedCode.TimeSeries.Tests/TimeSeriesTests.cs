using FluentAssertions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class TimeSeriesTests
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
    
    [Fact]
    public async Task AccumulatorByString()
    {
        var rnd = new Random();
        var series = new StringTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));

        var dt = DateTimeOffset.Now;
        series.AddNewData(dt, "1");
        series.AddNewData(dt, "1");
        series.AddNewData(dt, "2");
        series.AddNewData(dt, "3");
        series.AddNewData(dt, "3");
        series.AddNewData(dt, "2");


        dt = dt.AddHours(5);
        series.AddNewData(dt, "1");
        series.AddNewData(dt, "1");
        series.AddNewData(dt, "2");
        series.AddNewData(dt, "3");
        series.AddNewData(dt, "3");
        series.AddNewData(dt, "2");

        series.DataCount.Should().Be(12);
        series.Samples.First().Value.Count.Should().Be(3);
        series.Samples.Last().Value.Count.Should().Be(3);
    }

    [Fact]
    public async Task AccumulatorLimit()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
        for (var i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
        }

        series.SamplesCount.Should().Be(10);
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

        seriesA.Result.SamplesCount.Should().Be(10);
        seriesB.Result.SamplesCount.Should().Be(10);

        seriesA.Result.Merge(seriesB.Result);

        seriesA.Result.SamplesCount.Should().Be(10);

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

        onlineExpertsPerHourTimeSeries.SamplesCount.Should().Be(10);
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

        series.DataCount.Should().Be((ulong)count);
    }

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