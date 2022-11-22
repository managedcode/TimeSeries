// using System.Numerics;
//
// namespace ManagedCode.TimeSeries;
//
// public abstract class BaseGroupTimeSeriesSummer<TNumber, TSummer>
//     where TSummer : BaseTimeSeriesSummer<TNumber>, IDisposable
//     where TNumber : INumber<TNumber>
// {
//     private readonly Strategy _strategy;
//     private readonly bool _deleteOverdueSamples;
//     private readonly Timer _timer;
//     public readonly Dictionary<string, TSummer> TimeSeries = new();
//
//     protected BaseGroupTimeSeriesSummer(TimeSpan sampleInterval, bool deleteOverdueSamples)
//     {
//         if (deleteOverdueSamples)
//         {
//             _timer = new Timer(Callback, null, sampleInterval, sampleInterval);
//         }
//     }
//
//     private void Callback(object? state)
//     {
//         foreach (var summer in TimeSeries.ToArray())
//         {
//             summer.Value.DeleteOverdueSamples();
//             lock (TimeSeries)
//             {
//                 if (summer.Value.IsEmpty)
//                 {
//                     TimeSeries.Remove(summer.Key);
//                 }
//             }
//         }
//     }
//
//     public virtual void AddNewData(string key, TNumber value)
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.TryGetValue(key, out var summer))
//             {
//                 summer.AddNewData(value);
//             }
//             else
//             {
//                 var newSummer = CreateSummer();
//                 newSummer.AddNewData(value);
//                 TimeSeries[key] = newSummer;
//             }
//         }
//     }
//
//     public virtual void Increment(string key)
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.TryGetValue(key, out var summer))
//             {
//                 summer.Increment();
//             }
//             else
//             {
//                 var newSummer = CreateSummer();
//                 newSummer.Increment();
//                 TimeSeries[key] = newSummer;
//             }
//         }
//     }
//
//     public virtual void Decrement(string key)
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.TryGetValue(key, out var summer))
//             {
//                 summer.Decrement();
//             }
//             else
//             {
//                 var newSummer = CreateSummer();
//                 newSummer.Decrement();
//                 TimeSeries[key] = newSummer;
//             }
//         }
//     }
//
//     public abstract TNumber Average();
//
//     public abstract TNumber Min();
//
//     public abstract TNumber Max();
//
//     public abstract TNumber Sum();
//
//     protected abstract TSummer CreateSummer();
//
//     public void Dispose()
//     {
//         _timer?.Dispose();
//     }
// }