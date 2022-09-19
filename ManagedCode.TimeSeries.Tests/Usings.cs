global using Xunit;
using FluentAssertions;
using ManagedCode.TimeSeries.Sample;
using Newtonsoft.Json;

public class TimeSeriesTests
{
    [Fact]
    public async Task Accumulator()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));
        for (int i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
        }

        series.DataCount.Should().Be(1000);

        int step = 0;
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
    public async Task AccumulatorLimit()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1), 10);
        for (int i = 0; i < 1000; i++)
        {
            await Task.Delay(new Random().Next(1, 5));
            series.AddNewData(i);
        }

        series.SamplesCount.Should().Be(10);
    }
    
    [Fact]
    public void IntTimeSeriesSummerIncrementDecrement()
    {
        var series = new IntTimeSeriesSummer(TimeSpan.FromMinutes(1), 10);
        for (int i = 0; i < 100; i++)
        {
            series.Increment();
        }
        
        for (int i = 0; i < 50; i++)
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
        int count = 0;
        for (int i = 0; i < 100; i++)
        {
            await Task.Delay(new Random().Next(10, 50));
            series.AddNewData(i);
            count++;
        }

        series.DataCount.Should().Be((ulong)count);
    }
}