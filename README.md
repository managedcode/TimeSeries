![ManagedCode TimeSeries](https://raw.githubusercontent.com/managedcode/TimeSeries/main/logo.png)

# ManagedCode.TimeSeries

[![.NET](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/TimeSeries/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/TimeSeries?branch=main)
[![Release](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml)

> Lock-free, allocation-conscious, thread-safe time-series primitives for building fast counters, rolling analytics, and metric pipelines in .NET 10 / C# 14.

| Package | NuGet |
| --- | --- |
| Core library | [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.TimeSeries.svg)](https://www.nuget.org/packages/ManagedCode.TimeSeries) |

---

## What is this library?

ManagedCode.TimeSeries is a high-performance .NET time series metrics library. It provides optimized, time-bucketed
collections to aggregate events into **time series**:

- **Accumulators** store raw events per time bucket (queue per bucket).
- **Summers** store aggregated values per time bucket (sum/min/max/replace).
- **Groups** manage many series by key (per endpoint, per customer, per shard, etc.).

It's designed for high-throughput analytics: clicks, tokens, CPU usage, money, events, and any numeric telemetry.

### Why use it?

- Lock-free ingestion for high write rates.
- Thread-safe reads/writes for multi-threaded pipelines.
- UTC-normalized timestamps for deterministic ordering.
- Grouped series for multi-tenant metrics.
- Orleans-friendly converters and System.Text.Json helpers.

### Common use cases

- Web analytics (page views, clicks, conversion funnels).
- Infrastructure metrics (CPU, memory, latency, tokens).
- Billing counters (money, usage, events per window).

## Concepts (quick mental model)

- **SampleInterval**: bucket size (e.g., 1s, 10s, 1m).
- **Samples/Buckets**: ordered view of buckets keyed by **UTC timestamps** (`Buckets` is an alias of `Samples`).
- **MaxSamplesCount**: maximum bucket count per series; `0` means unbounded (this is not the event count).
- **DataCount**: total events processed (not the number of buckets).
- **UTC normalized**: you can pass any `DateTimeOffset`, but values are normalized to UTC internally; offsets are not stored.
- **Thread-safe**: concurrent reads/writes are supported across all public types.

## Install

```bash
dotnet add package ManagedCode.TimeSeries
```

## Quickstart

### 1) Make a rolling accumulator (store raw events)

```csharp
using ManagedCode.TimeSeries.Accumulators;

var requests = new IntTimeSeriesAccumulator(
    sampleInterval: TimeSpan.FromSeconds(5),
    maxSamplesCount: 60); // 60 buckets -> 5 minutes at 5s interval

// Use current time (UTC internally)
requests.Record(1);
requests.Record(1);

Console.WriteLine($"Buckets: {requests.Samples.Count}");
Console.WriteLine($"Events: {requests.DataCount}");
```

### 2) Make a summer (store aggregates per bucket)

```csharp
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Extensions;

var latency = new NumberTimeSeriesSummer<decimal>(TimeSpan.FromMilliseconds(500));

latency.Record(12.4m);
latency.Record(9.6m);

Console.WriteLine($"Sum: {latency.Sum()}");
Console.WriteLine($"Avg: {latency.Average():F2}");
Console.WriteLine($"Min/Max: {latency.Min()} / {latency.Max()}");
```

### 3) Track many keys at once (grouped series)

```csharp
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Extensions;

var perEndpoint = new IntGroupTimeSeriesAccumulator(
    sampleInterval: TimeSpan.FromSeconds(1),
    maxSamplesCount: 300,
    deleteOverdueSamples: true);

perEndpoint.AddNewData("/home", 1);
perEndpoint.AddNewData("/checkout", 1);

foreach (var (endpoint, accumulator) in perEndpoint.Snapshot())
{
    Console.WriteLine($"{endpoint}: {accumulator.DataCount} hits");
}
```

### 4) Serialize/deserialize with System.Text.Json

```csharp
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Serialization;

var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 10);
series.AddNewData(1);
series.AddNewData(2);

var json = TimeSeriesJsonSerializer.SerializeAccumulator<int, IntTimeSeriesAccumulator>(series);
var restored = TimeSeriesJsonSerializer.DeserializeAccumulator<int, IntTimeSeriesAccumulator>(json);

Console.WriteLine(restored.DataCount); // same as original
```

### 5) Orleans serialization (v9)

Converters in `ManagedCode.TimeSeries.Orleans` are marked with `[RegisterConverter]` and are auto-registered when the
assembly is loaded. Reference the package from your silo and client projects so the converters are available.
Converters are provided for int/float/double accumulators, summers, and grouped series, plus generic numeric summers.

## Time handling (DateTimeOffset vs DateTime)

- Public APIs accept `DateTimeOffset` so callers can pass any offset they have.
- Internally, timestamps are **normalized to UTC** (offset zero) for speed and determinism.
- If you need to preserve offsets, store them separately alongside your metric data.
- If you do not need custom timestamps, use `Record(value)` or `AddNewData(value)` and let the library use `DateTime.UtcNow`.

## Accumulators vs Summers

| Type | Stored per bucket | Best for |
| --- | --- | --- |
| Accumulator | `ConcurrentQueue<T>` | raw event lists, replay, exact values |
| Summer | `T` (aggregated) | fast stats, memory-efficient counters |

## Performance tips

- Keep `MaxSamplesCount` reasonable to limit memory.
- Use `Record(value)` (or `AddNewData(value)`) unless you must pass custom timestamps.
- Use summers for high-volume metrics (they avoid storing every event).
- Avoid locks in code that touches these structures.

## Design notes

- Accumulators use `ConcurrentQueue<T>` to remain lock-free and thread-safe under concurrent writes.
- Even in single-threaded runtimes, timers and background cleanup can introduce concurrency.
- If you need single-thread-only storage, consider using summers or ask for a dedicated single-thread accumulator.

## Architecture

- See `docs/Architecture/Overview.md` for detailed workflows, data model, and module boundaries.

## Development Workflow

```bash
dotnet restore ManagedCode.TimeSeries.slnx
dotnet build ManagedCode.TimeSeries.slnx --configuration Release
dotnet format ManagedCode.TimeSeries.slnx
dotnet build ManagedCode.TimeSeries.slnx --configuration Release
dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj --configuration Release
```

## Extensibility

| Scenario | Hook |
| --- | --- |
| Custom numeric type | Implement `INumber<T>` and use `NumberTimeSeriesSummer<T>` |
| Different aggregation | Use `Strategy` or implement a custom summer |
| Serialization | System.Text.Json helpers in `ManagedCode.TimeSeries.Serialization` |

## Contributing

1. Restore/build/test using the commands above.
2. Keep new APIs covered with tests (see existing samples in `ManagedCode.TimeSeries.Tests`).
3. Keep hot paths lock-free; only introduce locking when unavoidable and document the trade-off.
4. Update this README if you change behavior or public APIs.

## License

MIT (c) ManagedCode SAS.
