using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Serialization;
using ManagedCode.TimeSeries.Summers;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class SerializationTests
{
    [Fact]
    public void JsonAccumulatorRoundTripPreservesTotals()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var series = new IntTimeSeriesAccumulator(interval, maxSamplesCount: 5);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 10; i++)
        {
            series.AddNewData(start.AddTicks(interval.Ticks * i), i);
        }

        var json = TimeSeriesJsonSerializer.SerializeAccumulator<int, IntTimeSeriesAccumulator>(series);
        var restored = TimeSeriesJsonSerializer.DeserializeAccumulator<int, IntTimeSeriesAccumulator>(json);

        restored.DataCount.ShouldBe(series.DataCount);
        restored.Samples.Count.ShouldBe(series.Samples.Count);
        restored.Start.Offset.ShouldBe(TimeSpan.Zero);
        restored.End.Offset.ShouldBe(TimeSpan.Zero);
        restored.LastDate.Offset.ShouldBe(TimeSpan.Zero);

        var originalSum = series.Samples.Sum(pair => pair.Value.Sum());
        var restoredSum = restored.Samples.Sum(pair => pair.Value.Sum());
        restoredSum.ShouldBe(originalSum);
    }

    [Fact]
    public void JsonSummerRoundTripPreservesStrategy()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var summer = new DoubleTimeSeriesSummer(interval, maxSamplesCount: 4, strategy: Strategy.Max);
        var start = DateTimeOffset.UtcNow;

        summer.AddNewData(start, 1.5);
        summer.AddNewData(start.AddTicks(interval.Ticks), 2.5);
        summer.AddNewData(start.AddTicks(interval.Ticks * 2), 0.5);

        var json = TimeSeriesJsonSerializer.SerializeSummer<double, DoubleTimeSeriesSummer>(summer);
        var restored = TimeSeriesJsonSerializer.DeserializeSummer<double, DoubleTimeSeriesSummer>(json);

        restored.Strategy.ShouldBe(Strategy.Max);
        restored.Sum().ShouldBe(summer.Sum());
        restored.Max().ShouldBe(summer.Max());
        restored.Min().ShouldBe(summer.Min());
        restored.Start.Offset.ShouldBe(TimeSpan.Zero);
    }
}
