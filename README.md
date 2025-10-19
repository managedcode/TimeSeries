![ManagedCode TimeSeries](https://raw.githubusercontent.com/managedcode/TimeSeries/main/logo.png)

# ManagedCode.TimeSeries

[![.NET](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/TimeSeries/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/TimeSeries?branch=main)
[![Release](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml)

> Lock-free, allocation-conscious time-series primitives written in modern C# for building fast counters, aggregations, and rolling analytics.

| Package | NuGet |
| --- | --- |
| Core library | [![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.TimeSeries.svg)](https://www.nuget.org/packages/ManagedCode.TimeSeries) |

---

## Why TimeSeries?

- **Data pipelines demand concurrency.** We built the core on lock-free `ConcurrentDictionary`/`ConcurrentQueue` structures and custom atomic helpers, so write-heavy workloads scale across cores without blocking.
- **Numeric algorithms shouldn’t duplicate code.** Everything is generic over `INumber<T>`, so `int`, `decimal`, or your own numeric type can use the same summer/accumulator implementations.
- **Hundreds of signals, one API.** Grouped accumulators and summers make it trivial to manage keyed windows (think “per customer”, “per endpoint”, “per shard”) with automatic clean-up.
- **Production-ready plumbing.** Orleans converters, Release automation, Coveralls reporting, and central package management are all wired up out of the box.

## Table of Contents

1. [Feature Highlights](#feature-highlights)  
2. [Quickstart](#quickstart)  
3. [Architecture Notes](#architecture-notes)  
4. [Development Workflow](#development-workflow)  
5. [Release Automation](#release-automation)  
6. [Extensibility](#extensibility)  
7. [Contributing](#contributing)  
8. [License](#license)

## Feature Highlights

- **Lock-free core** – writes hit `ConcurrentDictionary` + `ConcurrentQueue`, range metadata updated via custom atomics.
- **Generic summers** – `NumberTimeSeriesSummer<T>` & friends operate on any `INumber<T>` implementation.
- **Mass fan-in ready** – grouped accumulators/summers handle hundreds of keys without `lock`.
- **Orleans-native** – converters bridge to Orleans surrogates so grains can persist accumulators out of the box.
- **Delivery pipeline** – GitHub Actions release workflow bundles builds, tests, packs, tagging, and publishing.
- **Central package versions** – single source of NuGet truth via `Directory.Packages.props`.

## Quickstart

### Install

```bash
dotnet add package ManagedCode.TimeSeries
```

### Create a rolling accumulator

```csharp
using ManagedCode.TimeSeries.Accumulators;

var requests = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(5), maxSamplesCount: 60);

Parallel.For(0, 10_000, i =>
{
    requests.AddNewData(i);
});

Console.WriteLine($"Samples stored: {requests.Samples.Count}");
Console.WriteLine($"Events processed: {requests.DataCount}");
```

### Summaries with any numeric type

```csharp
using ManagedCode.TimeSeries.Summers;

var latency = new NumberTimeSeriesSummer<decimal>(TimeSpan.FromMilliseconds(500));

latency.AddNewData(DateTimeOffset.UtcNow, 12.4m);
latency.AddNewData(DateTimeOffset.UtcNow.AddMilliseconds(250), 9.6m);

Console.WriteLine($"AVG: {latency.Average():F2} ms");
Console.WriteLine($"P50/P100: {latency.Min()} / {latency.Max()}");
```

### Track many signals at once

```csharp
using ManagedCode.TimeSeries.Accumulators;

var perEndpoint = new IntGroupTimeSeriesAccumulator(
    sampleInterval: TimeSpan.FromSeconds(1),
    maxSamplesCount: 300,
    deleteOverdueSamples: true);

Parallel.ForEach(requests, req =>
{
    perEndpoint.AddNewData(req.Path, req.Timestamp, 1);
});

foreach (var (endpoint, accumulator) in perEndpoint.Snapshot())
{
    Console.WriteLine($"{endpoint}: {accumulator.DataCount} hits");
}
```

### Orleans-friendly serialization

```csharp
// In your Orleans silo:
builder.Services.AddSerializer(builder =>
{
    builder.AddConverter<IntTimeSeriesAccumulatorConverter<int>>();
    builder.AddConverter<IntTimeSeriesSummerConverter<int>>();
    // …add others as needed
});
```

## Architecture Notes

- **Lock-free core:** `BaseTimeSeries` stores samples in a `ConcurrentDictionary` and updates range metadata through `AtomicDateTimeOffset`. Per-key data is a `ConcurrentQueue<T>` (accumulators) or direct `INumber<T>` (summers).
- **Deterministic reads:** consumers get an ordered read-only projection of the concurrent map, so existing iteration/test semantics stay intact while writers remain lock-free.
- **Group managers:** `BaseGroupTimeSeriesAccumulator` and `BaseGroupNumberTimeSeriesSummer` use `ConcurrentDictionary<string, ...>` plus lightweight background timers for overdue clean-up—no `lock` statements anywhere on the hot path.
- **Orleans bridge:** converters project between the concurrent structures and Orleans’ plain dictionaries/queues, keeping serialized payloads simple while the live types stay lock-free.

## Extensibility

| Scenario | Hook |
| --- | --- |
| Custom numeric type | Implement `INumber<T>` and plug into `NumberTimeSeriesSummer<T>` |
| Alternative aggregation strategy | Extend `Strategy` enum & override `Update` in a derived summer |
| Domain-specific accumulator | Derive from `TimeSeriesAccumulator<T, TSelf>` (future rename of `BaseTimeSeriesAccumulator`) and expose tailored helpers |
| Serialization | Add dedicated Orleans converters / System.Text.Json converters using the pattern in `ManagedCode.TimeSeries.Orleans` |

> **Heads up:** the `Base*` prefixes hang around for historical reasons. We plan to rename the concrete-ready generics to `TimeSeriesAccumulator<T,...>` / `TimeSeriesSummer<T,...>` in a future release with deprecation shims.

## Development Workflow

- Solution: `ManagedCode.TimeSeries.slnx`
  ```bash
  dotnet restore ManagedCode.TimeSeries.slnx
  dotnet build ManagedCode.TimeSeries.slnx --configuration Release
  dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj --configuration Release
  ```
- Packages: update versions only in `Directory.Packages.props`.
- Coverage: `dotnet test ... -p:CollectCoverage=true -p:CoverletOutputFormat=lcov`.
- Benchmarks: `dotnet run --project ManagedCode.TimeSeries.Benchmark --configuration Release`.

## Release Automation

- Workflow: `.github/workflows/release.yml`
  - Trigger: push to `main` or manual `workflow_dispatch`.
  - Steps: restore → build → test → pack → `dotnet nuget push` (skip duplicates) → create/tag release.
  - Configure secrets:
    - `NUGET_API_KEY`: NuGet publish token.
    - Default `${{ secrets.GITHUB_TOKEN }}` is used for tagging and releases.

## Contributing

1. Restore/build/test using the commands above.
2. Keep new APIs covered with tests (see existing samples in `ManagedCode.TimeSeries.Tests`).
3. Align with the lock-free architecture—avoid introducing `lock` on hot paths.
4. Document new features in this README.

## License

MIT © ManagedCode SAS.
