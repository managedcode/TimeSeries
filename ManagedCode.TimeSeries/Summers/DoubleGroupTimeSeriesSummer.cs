// namespace ManagedCode.TimeSeries.Summers;
//
// public class DoubleGroupTimeSeriesSummer : BaseGroupTimeSeriesSummer<double, DoubleTimeSeriesSummer>
// {
//     private readonly TimeSpan _sampleInterval;
//     private readonly int _samplesCount;
//     private readonly Strategy _strategy;
//
//     public DoubleGroupTimeSeriesSummer(TimeSpan sampleInterval, int samplesCount,  Strategy strategy, bool deleteOverdueSamples) : base(sampleInterval, deleteOverdueSamples)
//     {
//         _sampleInterval = sampleInterval;
//         _samplesCount = samplesCount;
//         _strategy = strategy;
//     } 
//     
//     public override double Average()
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.Count == 0)
//                 return 0;
//             
//             return TimeSeries.Select(s => s.Value.Average()).Average();
//         }
//     }
//
//     public override double Min()
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.Count == 0)
//                 return 0;
//             
//             return TimeSeries.Select(s => s.Value.Min()).Min();
//         }
//     }
//
//     public override double Max()
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.Count == 0)
//                 return 0;
//             
//             return TimeSeries.Select(s => s.Value.Max()).Max();
//         }
//     }
//
//     public override double Sum()
//     {
//         lock (TimeSeries)
//         {
//             if (TimeSeries.Count == 0)
//                 return 0;
//             
//             return TimeSeries.Select(s => s.Value.Sum()).Sum();
//         }
//     }
//
//     protected override DoubleTimeSeriesSummer CreateSummer()
//     {
//         throw new NotImplementedException();
//     }
// }