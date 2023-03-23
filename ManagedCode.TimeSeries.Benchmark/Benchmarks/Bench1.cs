using BenchmarkDotNet.Attributes;
using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Benchmark.Benchmarks;


[SimpleJob]
[MemoryDiagnoser]
public class Bench1
{
    [Benchmark]
    public async Task Int_1000()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(50));
        for (var i = 0; i < 1000; i++)
        {
            series.AddNewData(i);
        }
    }
    
    [Benchmark]
    public async Task Int_100_000()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(50));
        for (var i = 0; i < 100_000; i++)
        {
            series.AddNewData(i);
        }
    }
    
    [Benchmark]
    public async Task Int_10_000_000()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(50));
        for (var i = 0; i < 10_000_000; i++)
        {
            series.AddNewData(i);
        }
    }
}