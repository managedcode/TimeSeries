![img|300x200](https://raw.githubusercontent.com/managedcode/TimeSeries/main/logo.png)

# TimeSeries

[![.NET](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/TimeSeries/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/TimeSeries/badge.svg?branch=main&service=github)](https://coveralls.io/github/managedcode/TimeSeries?branch=main)
[![nuget](https://github.com/managedcode/TimeSeries/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/TimeSeries/actions/workflows/nuget.yml)
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
- Summers for summarizing data in time frames.
- Efficient storage and compression of time series data.

## Example
Here's an example of how you might use the TimeSeries library to accumulate and summarize data in a time frame:

```csharp
using ManagedCode.TimeSeries;

var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(5)); // step
for (int i = 0; i < count; i++)
{
    series.AddNewData(i);
}
```

```csharp
using ManagedCode.TimeSeries;

var series = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(0.1));
for (var i = 0; i < 1000; i++)
{
    await Task.Delay(new Random().Next(1, 5));
    series.AddNewData(i);
}

series.DataCount; // 1000
```

## Installation
To install the TimeSeries library, you can use NuGet:

```bash
dotnet add package ManagedCode.TimeSeries
```


Conclusion
In summary, the TimeSeries library provides convenient tools for working with time series data in C#. 
Its accumulators and summers make it easy to add and summarize data in time frames, and its efficient storage and compression capabilities ensure.