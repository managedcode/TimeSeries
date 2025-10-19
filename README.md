![img|300x200](https://raw.githubusercontent.com/managedcode/TimeSeries/main/logo.png)

# TimeSeries

[![.NET](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/TimeSeries/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/TimeSeries?branch=main)
[![Release](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/release.yml)
[![CodeQL](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/codeql-analysis.yml)

| Version | Package                                                                                                                             | Description     |
| ------- |-------------------------------------------------------------------------------------------------------------------------------------|-----------------|
|[![NuGet Package](https://img.shields.io/nuget/v/ManagedCode.TimeSeries.svg)](https://www.nuget.org/packages/ManagedCode.TimeSeries) | [ManagedCode.TimeSeries](https://www.nuget.org/packages/ManagedCode.TimeSeries)                                                   | Core            |

---

## Motivation
Time series data is a common type of data in many applications, such as finance, physics, and engineering. 
It is often necessary to store and manipulate large amounts of time series data efficiently in order to perform analysis and make predictions.

Our C# library, TimeSeries, provides convenient tools for working with time series data in C#. 
It includes classes for accumulating and summarizing data in time frames, as well as storing and compressing the data efficiently. This makes it easy to add and manage time series data in your C# projects.

## Features
- Accumulators for adding data to time frames.
- Generic numeric summers via `NumberTimeSeriesSummer<T>` and `NumberGroupTimeSeriesSummer<T>` built on the `INumber<T>` abstractions.
- Group collections (`IntGroupTimeSeriesAccumulator`, `DoubleGroupTimeSeriesAccumulator`, etc.) for managing hundreds of keyed windows with automatic cleanup.
- Efficient storage and compression of time series data.
- Automated release workflow that builds, packs, tags, and publishes NuGet packages straight from the `main` branch.

## Examples
Add raw data to a per-sample accumulator:

```csharp
using ManagedCode.TimeSeries.Accumulators;

var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(5));
for (var i = 0; i < count; i++)
{
    series.AddNewData(i);
}
```

Build numeric aggregations with any `INumber` type:

```csharp
using ManagedCode.TimeSeries.Summers;

var summer = new NumberTimeSeriesSummer<long>(TimeSpan.FromSeconds(1));
var now = DateTimeOffset.UtcNow;

summer.AddNewData(now, 10);
summer.AddNewData(now.AddSeconds(1), 15);

var total = summer.Sum();      // 25
var min = summer.Min();        // 10
var average = summer.Average(); // 12
```

Manage multiple keyed streams simultaneously:

```csharp
using ManagedCode.TimeSeries.Accumulators;

var groups = new IntGroupTimeSeriesAccumulator(TimeSpan.FromMinutes(1), maxSamplesCount: 1440);

groups.AddNewData("checkout", DateTimeOffset.UtcNow, 1);
groups.AddNewData("checkout", DateTimeOffset.UtcNow.AddMinutes(1), 3);
groups.AddNewData("search", DateTimeOffset.UtcNow, 12);

var snapshot = groups.Snapshot();
```

## Installation
To install the TimeSeries library, you can use NuGet:

```bash
dotnet add package ManagedCode.TimeSeries
```

## Release automation
- The `release` workflow runs on every push to `main` (or via manual dispatch).
- It restores, builds, tests, and packs all projects using the centrally managed package versions in `Directory.Packages.props`.
- Define the `NUGET_API_KEY` GitHub secret with a NuGet publishing token before triggering the workflow.
- Successful runs push packages to NuGet (skipping duplicates), create an annotated `v{Version}` git tag, and publish GitHub Release notes with package links.

## Development notes
- NuGet dependencies are managed centrally in `Directory.Packages.props`; update versions there rather than inside individual project files.
- Run `dotnet build --configuration Release` and `dotnet test` before sending changes—analyzers run as part of the build.
- The updated accumulators and summers share a common `INumber<T>` backbone, so you can add custom numeric types by implementing `INumber<T>`.
- The primary solution is `ManagedCode.TimeSeries.slnx`; use it with `dotnet restore`/`dotnet build` for consistent results in CI and locally.


## Conclusion
In summary, the TimeSeries library provides convenient tools for working with time series data in C#. 
Its accumulators and summers make it easy to add and summarize data in time frames, their new generic counterparts remove duplicated integer/double/float code, and the automated release pipeline keeps NuGet packages up to date with minimal effort.
