using BenchmarkDotNet.Attributes;
using ManagedCode.TimeSeries.Accumulators;

namespace ManagedCode.TimeSeries.Benchmark.Benchmarks;


[SimpleJob]
[MemoryDiagnoser]
public class CollectionBenchmark
{
    [Benchmark]
    public void Queue_1000()
    {
        var series = new Queue<int>();
        for (var i = 0; i < 1000; i++)
        {
            series.Enqueue(i);
            if (series.Count > 100)
                series.Dequeue();
        }
    }
    
    [Benchmark]
    public void Queue_100_000()
    {
        var series = new Queue<int>();
        for (var i = 0; i < 100_000; i++)
        {
            series.Enqueue(i);
            if (series.Count > 100)
                series.Dequeue();
        }
    }
    
    [Benchmark]
    public void Queue_10_000_000()
    {
        var series = new Queue<int>();
        for (var i = 0; i < 10_000_000; i++)
        {
            series.Enqueue(i);
            if (series.Count > 100)
                series.Dequeue();
        }
    }
    
    [Benchmark]
    public void LinkedList_1000()
    {
        var series = new LinkedList<int>();
        for (var i = 0; i < 1000; i++)
        {
            series.AddLast(i);
            if (series.Count > 100)
                series.RemoveFirst();
        }
    }
    
    [Benchmark]
    public void LinkedList_100_000()
    {
        var series = new LinkedList<int>();
        for (var i = 0; i < 100_000; i++)
        {
            series.AddLast(i);
            if (series.Count > 100)
                series.RemoveFirst();
        }
    }
    
    [Benchmark]
    public void LinkedList_10_000_000()
    {
        var series = new LinkedList<int>();
        for (var i = 0; i < 10_000_000; i++)
        {
            series.AddLast(i);
            if (series.Count > 100)
                series.RemoveFirst();
        }
    }
    
    [Benchmark]
    public void List_1000()
    {
        var series = new List<int>();
        for (var i = 0; i < 1000; i++)
        {
            series.Add(i);
            if (series.Count > 100)
                series.RemoveAt(0);
        }
    }
    
    [Benchmark]
    public void List_100_000()
    {
        var series = new List<int>();
        for (var i = 0; i < 100_000; i++)
        {
            series.Add(i);
            if (series.Count > 100)
                series.RemoveAt(0);
        }
    }
    
    [Benchmark]
    public void List_10_000_000()
    {
        var series = new List<int>();
        for (var i = 0; i < 10_000_000; i++)
        {
            series.Add(i);
            if (series.Count > 100)
                series.RemoveAt(0);
        }
    }
}