using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Extensions;
using ManagedCode.TimeSeries.Orleans;
using ManagedCode.TimeSeries.Tests.Assertions;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class TimeSeriesBehaviorTests
{
    private sealed class TestAccumulator : BaseTimeSeriesAccumulator<int, TestAccumulator>
    {
        public TestAccumulator(TimeSpan sampleInterval, int maxSamplesCount)
            : base(sampleInterval, maxSamplesCount)
        {
        }

        public TestAccumulator(TimeSpan sampleInterval, int maxSamplesCount, DateTimeOffset start, DateTimeOffset end, DateTimeOffset last)
            : base(sampleInterval, maxSamplesCount, start, end, last)
        {
        }

        public IReadOnlyDictionary<DateTimeOffset, ConcurrentQueue<int>> InternalStorage => Storage;

        public void ForceReset() => ResetSamplesStorage();

        public void ForceSetMaxSamples(int value) => MaxSamplesCount = value;
    }

    private sealed class StringSetAccumulator : BaseTimeSeriesByValueAccumulator<string, StringSetAccumulator>
    {
        public StringSetAccumulator(TimeSpan sampleInterval, int samplesCount = 0)
            : base(sampleInterval, samplesCount)
        {
        }

        public override void Resample(TimeSpan sampleInterval, int samplesCount)
        {
            if (sampleInterval <= SampleInterval)
            {
                throw new InvalidOperationException();
            }

            SampleInterval = sampleInterval;
            MaxSamplesCount = samplesCount;

            var snapshot = Storage.ToArray();
            ResetSamplesStorage();
            foreach (var (key, set) in snapshot)
            {
                foreach (var value in set.Keys)
                {
                    AddNewData(key, value);
                }
            }
        }
    }

    [Fact]
    public void Accumulator_DropsOldestEntriesWhenCapacityExceeded()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(1), maxSamplesCount: 3);
        var start = DateTimeOffset.UnixEpoch;

        for (var i = 0; i < 6; i++)
        {
            accumulator.AddNewData(start.AddMilliseconds(i), i);
        }

        accumulator.Samples.Count.ShouldBe(3);
        accumulator.Samples.Keys.ShouldSequenceEqual(
            start.AddMilliseconds(3),
            start.AddMilliseconds(4),
            start.AddMilliseconds(5));
    }

    [Fact]
    public void Accumulator_DeleteOverdueSamples_RemovesObsoleteWindows()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(100), maxSamplesCount: 4);
        var baseTime = DateTimeOffset.UtcNow.AddSeconds(-10);

        for (var i = 0; i < 8; i++)
        {
            accumulator.AddNewData(baseTime.AddMilliseconds(i * 100), i);
        }

        accumulator.Samples.Count.ShouldBe(4);
        accumulator.DeleteOverdueSamples();
        accumulator.Samples.Count.ShouldBeLessThanOrEqualTo(4);
        accumulator.Samples.Keys.ShouldBeInAscendingOrder();
    }

    [Fact]
    public void Accumulator_ResetSamples_ClearsState()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(1), maxSamplesCount: 4);
        var now = DateTimeOffset.UtcNow;
        accumulator.AddNewData(now, 1);
        accumulator.InternalStorage.ShouldNotBeEmpty();

        accumulator.ForceReset();

        accumulator.Samples.ShouldBeEmpty();
        accumulator.Start.ShouldBeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        accumulator.End.ShouldBeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Accumulator_IsOverflowPropertyAccessible()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(1), maxSamplesCount: 1);
        accumulator.AddNewData(DateTimeOffset.UtcNow, 42);
        accumulator.AddNewData(DateTimeOffset.UtcNow.AddMilliseconds(1), 43);
        accumulator.ForceSetMaxSamples(-1);
        var overflow = accumulator.IsOverflow;
        overflow.ShouldBeFalse();
    }

    [Fact]
    public void Accumulator_RebaseCombinesMultipleSources()
    {
        var source1 = new TestAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 16);
        var source2 = new TestAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 16);
        var baseline = DateTimeOffset.UnixEpoch;

        source1.AddNewData(baseline, 1);
        source1.AddNewData(baseline.AddMilliseconds(10), 2);

        source2.AddNewData(baseline.AddMilliseconds(20), 3);
        source2.AddNewData(baseline.AddMilliseconds(30), 4);

        var seed = new TestAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 32);
        var rebase = seed.Rebase(new[] { source1, source2 });

        rebase.Samples.Count.ShouldBe(4);
        rebase.DataCount.ShouldBe(4ul);
        rebase.Samples.Keys.ShouldSequenceEqual(
            baseline,
            baseline.AddMilliseconds(10),
            baseline.AddMilliseconds(20),
            baseline.AddMilliseconds(30));
    }

    [Fact]
    public void Accumulator_MergeEnumerableAggregatesSources()
    {
        var target = new TestAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 16);
        var others = Enumerable.Range(0, 3).Select(index =>
        {
            var acc = new TestAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 16);
            acc.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(index * 10), index);
            return acc;
        }).ToArray();

        target.Merge(others);

        target.DataCount.ShouldBe(3ul);
        target.Samples.Count.ShouldBe(3);
    }

    [Fact]
    public void Accumulator_OperatorPlus_ProducesMergedSnapshot()
    {
        var left = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var right = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);

        left.AddNewData(DateTimeOffset.UnixEpoch, 1);
        right.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(5), 2);

        var merged = left + right;
        merged.Samples.Count.ShouldBe(right.Samples.Count);
        merged.DataCount.ShouldBe(right.DataCount);
    }

    [Fact]
    public void Accumulator_CheckedAddition_ProducesMergedSnapshot()
    {
        var left = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var right = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);

        left.AddNewData(DateTimeOffset.UnixEpoch, 1);
        right.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(5), 2);

        var merged = checked(left + right);
        merged.Samples.Count.ShouldBe(right.Samples.Count);
        merged.DataCount.ShouldBe(right.DataCount);
    }

    [Fact]
    public void StringAccumulator_Merge_UnionsValues()
    {
        var left = new StringSetAccumulator(TimeSpan.FromSeconds(1));
        var right = new StringSetAccumulator(TimeSpan.FromSeconds(1));
        var ts = DateTimeOffset.UtcNow;

        left.AddNewData(ts, "alpha");
        left.AddNewData(ts, "beta");
        right.AddNewData(ts, "beta");
        right.AddNewData(ts, "gamma");

        left.Merge(right);

        left.Samples.Count.ShouldBe(1);
        var values = left.Samples.First().Value;
        values.ShouldContainKeys("alpha", "beta", "gamma");
    }

    [Fact]
    public void StringAccumulator_ResampleSmallerIntervalThrows()
    {
        var accumulator = new StringSetAccumulator(TimeSpan.FromSeconds(1));
        accumulator.AddNewData(DateTimeOffset.UtcNow, "value");

        Action act = () => accumulator.Resample(TimeSpan.FromMilliseconds(100), 4);
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void GroupAccumulator_TryGetAndRemove_BehavesAsExpected()
    {
        var group = new IntGroupTimeSeriesAccumulator(
            sampleInterval: TimeSpan.FromMilliseconds(5),
            maxSamplesCount: 32,
            deleteOverdueSamples: false);

        group.AddNewData("alpha", DateTimeOffset.UtcNow, 1);

        group.TryGet("alpha", out var first).ShouldBeTrue();
        first.ShouldNotBeNull();
        group.Remove("alpha").ShouldBeTrue();
        group.TryGet("alpha", out _).ShouldBeFalse();
    }

    [Fact]
    public void NumberGroupSummer_NoData_ReturnsZeros()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(
            sampleInterval: TimeSpan.FromSeconds(1),
            samplesCount: 16,
            strategy: Strategy.Sum,
            deleteOverdueSamples: false);

        group.Sum().ShouldBe(0);
        group.Average().ShouldBe(0);
        group.Min().ShouldBe(0);
        group.Max().ShouldBe(0);
    }

    [Fact]
    public void NumberGroupSummer_IncrementAndDecrement_Work()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(
            sampleInterval: TimeSpan.FromSeconds(1),
            samplesCount: 16,
            strategy: Strategy.Sum,
            deleteOverdueSamples: false);

        group.Increment("key");
        group.Decrement("key");

        group.Sum().ShouldBe(0);
        group.Max().ShouldBe(0);
        group.Min().ShouldBe(0);
    }

    [Fact]
    public void NumberSummer_DecrementProducesNegativeValues()
    {
        var summer = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(10));
        summer.Decrement();
        summer.Sum().ShouldBe(-1);
        summer.Min().ShouldBe(-1);
        summer.Max().ShouldBe(-1);
    }

    [Fact]
    public void NumberSummer_EmptyFactoryCreatesInstance()
    {
        var empty = NumberTimeSeriesSummer<int>.Empty(TimeSpan.FromSeconds(2), maxSamplesCount: 5);
        empty.ShouldNotBeNull();
        empty.Samples.ShouldBeEmpty();
    }

    [Fact]
    public void OrleansAccumulatorConverter_RoundTripsEmptySeries()
    {
        var converter = new IntTimeSeriesAccumulatorConverter<int>();
        var source = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 4);

        var surrogate = converter.ConvertToSurrogate(source);
        surrogate.Samples.ShouldBeEmpty();

        var restored = converter.ConvertFromSurrogate(in surrogate);
        restored.Samples.ShouldBeEmpty();
        restored.DataCount.ShouldBe(0ul);
    }

    [Fact]
    public void OrleansConverters_HandleFloatAndDouble()
    {
        var floatAcc = new FloatTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 8);
        floatAcc.AddNewData(DateTimeOffset.UnixEpoch, 1.23f);
        var floatConverter = new FloatTimeSeriesAccumulatorConverter<float>();
        var floatSurrogate = floatConverter.ConvertToSurrogate(floatAcc);
        var floatRestored = floatConverter.ConvertFromSurrogate(floatSurrogate);
        var floatOriginal = floatAcc.Samples.Sum(pair => pair.Value.Sum());
        var floatRoundTripped = floatRestored.Samples.Sum(pair => pair.Value.Sum());
        floatRoundTripped.ShouldBe(floatOriginal);

        var doubleAcc = new DoubleTimeSeriesAccumulator(TimeSpan.FromMilliseconds(10), maxSamplesCount: 6);
        doubleAcc.AddNewData(DateTimeOffset.UnixEpoch, 4.2);
        var doubleAccConverter = new DoubleTimeSeriesAccumulatorConverter<double>();
        var doubleAccSurrogate = doubleAccConverter.ConvertToSurrogate(doubleAcc);
        var doubleAccRestored = doubleAccConverter.ConvertFromSurrogate(doubleAccSurrogate);
        doubleAccRestored.DataCount.ShouldBe(doubleAcc.DataCount);
        doubleAccRestored.Samples.Count.ShouldBe(doubleAcc.Samples.Count);

        var doubleSummer = new DoubleTimeSeriesSummer(TimeSpan.FromMilliseconds(10), maxSamplesCount: 8, Strategy.Sum);
        doubleSummer.AddNewData(DateTimeOffset.UnixEpoch, 3.14);
        var doubleConverter = new DoubleTimeSeriesSummerConverter<double>();
        var doubleSurrogate = doubleConverter.ConvertToSurrogate(doubleSummer);
        var doubleRestored = doubleConverter.ConvertFromSurrogate(doubleSurrogate);
        doubleRestored.Sum().ShouldBe(doubleSummer.Sum());

        var floatSummer = new FloatTimeSeriesSummer(TimeSpan.FromMilliseconds(10), maxSamplesCount: 6, Strategy.Max);
        floatSummer.AddNewData(DateTimeOffset.UnixEpoch, 1.0f);
        floatSummer.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(10), 2.0f);
        var floatSummerConverter = new FloatTimeSeriesSummerConverter<float>();
        var floatSummerSurrogate = floatSummerConverter.ConvertToSurrogate(floatSummer);
        var floatSummerRestored = floatSummerConverter.ConvertFromSurrogate(floatSummerSurrogate);
        floatSummerRestored.Max().ShouldBe(floatSummer.Max());
    }

    [Fact]
    public void OrleansSummerConverter_RoundTripsEmptySeries()
    {
        var converter = new IntTimeSeriesSummerConverter<int>();
        var summer = new IntTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 4, strategy: Strategy.Sum);

        var surrogate = converter.ConvertToSurrogate(summer);
        surrogate.Samples.ShouldBeEmpty();

        var restored = converter.ConvertFromSurrogate(in surrogate);
        restored.Samples.ShouldBeEmpty();
        restored.Sum().ShouldBe(0);
    }

    [Fact]
    public void Accumulator_StartEndMutatorsInvokableViaReflection()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var baseType = typeof(BaseTimeSeries<int, ConcurrentQueue<int>, TestAccumulator>);
        var startSetter = baseType.GetMethod("set_Start", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var endSetter = baseType.GetMethod("set_End", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var newStart = DateTimeOffset.UnixEpoch.AddMinutes(1);
        var newEnd = DateTimeOffset.UnixEpoch.AddMinutes(2);

        startSetter.Invoke(accumulator, new object[] { newStart });
        endSetter.Invoke(accumulator, new object[] { newEnd });

        accumulator.Start.ShouldBe(newStart);
        accumulator.End.ShouldBe(newEnd);
    }

    [Fact]
    public void Accumulator_SamplesExposeDictionarySemantics()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var timestamp = DateTimeOffset.UnixEpoch;
        var secondTimestamp = timestamp.AddMilliseconds(10);

        accumulator.AddNewData(timestamp, 99);
        accumulator.AddNewData(secondTimestamp, 100);

        var samples = accumulator.Samples;
        samples.ShouldHaveCount(2);

        var roundedFirst = timestamp.Round(TimeSpan.FromMilliseconds(5));
        var roundedSecond = secondTimestamp.Round(TimeSpan.FromMilliseconds(5));

        samples.ContainsKey(roundedFirst).ShouldBeTrue();
        samples.TryGetValue(roundedFirst, out var firstQueue).ShouldBeTrue();
        firstQueue.ShouldNotBeNull();
        firstQueue!.ShouldContain(99);

        samples.TryGetValue(roundedSecond, out var secondQueue).ShouldBeTrue();
        secondQueue.ShouldNotBeNull();
        secondQueue!.ShouldContain(100);

        samples.TryGetValue(secondTimestamp.AddMilliseconds(1), out _).ShouldBeFalse();
        samples.Keys.ShouldBeInAscendingOrder();

        var enumerator = ((IEnumerable)samples).GetEnumerator();
        enumerator.MoveNext().ShouldBeTrue();
        enumerator.MoveNext().ShouldBeTrue();
    }

    [Fact]
    public void Accumulator_CanBeConstructedWithExplicitRange()
    {
        var start = DateTimeOffset.UnixEpoch.AddMinutes(1);
        var end = start.AddMinutes(5);
        var last = end.AddSeconds(30);

        var accumulator = new TestAccumulator(TimeSpan.FromSeconds(1), 16, start, end, last);
        accumulator.Start.ShouldBe(start.Round(TimeSpan.FromSeconds(1)));
        accumulator.End.ShouldBe(end.Round(TimeSpan.FromSeconds(1)));
        accumulator.LastDate.ShouldBe(last);
    }

    [Fact]
    public void AtomicDateTimeOffset_DefaultReadReturnsDefault()
    {
        var assembly = typeof(BaseTimeSeries<,,>).Assembly;
        var atomicType = assembly.GetType("ManagedCode.TimeSeries.Abstractions.AtomicDateTimeOffset", throwOnError: true)!;
        var atomic = Activator.CreateInstance(atomicType);
        var readMethod = atomicType.GetMethod("Read", BindingFlags.Public | BindingFlags.Instance)!;
        var result = (DateTimeOffset)readMethod.Invoke(atomic, Array.Empty<object>())!;
        result.ShouldBe(default);
    }

    [Fact]
    public void TimeSeriesEmpty_WithoutInterval_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => IntTimeSeriesAccumulator.Empty());
        Should.Throw<ArgumentOutOfRangeException>(() => IntTimeSeriesAccumulator.Empty(TimeSpan.Zero));

        var empty = IntTimeSeriesAccumulator.Empty(TimeSpan.FromMilliseconds(10));
        empty.Samples.ShouldBeEmpty();
        empty.SampleInterval.ShouldBe(TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public void AccumulatorResample_PreservesDataCountAndLastDate()
    {
        var series = new IntTimeSeriesAccumulator(TimeSpan.FromMilliseconds(25), maxSamplesCount: 8);
        var start = DateTimeOffset.UtcNow;

        for (var i = 0; i < 6; i++)
        {
            series.AddNewData(start.AddMilliseconds(i * 25), i);
        }

        var countBefore = series.DataCount;
        var lastBefore = series.LastDate;

        series.Resample(TimeSpan.FromMilliseconds(50), samplesCount: 4);

        series.DataCount.ShouldBe(countBefore);
        series.LastDate.ShouldBe(lastBefore.Round(TimeSpan.FromMilliseconds(50)));
        series.Samples.Count.ShouldBeLessThanOrEqualTo(4);
        series.Samples.Values.Sum(queue => queue.Count).ShouldBe((int)countBefore);
    }

    [Fact]
    public void GroupAccumulatorCallback_RemovesExpiredEntries()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var group = new IntGroupTimeSeriesAccumulator(interval, maxSamplesCount: 1, deleteOverdueSamples: true);

        var staleTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1));
        group.AddNewData("expired", staleTimestamp, 42);

        var callback = typeof(BaseGroupTimeSeriesAccumulator<int, IntTimeSeriesAccumulator>)
            .GetMethod("Callback", BindingFlags.Instance | BindingFlags.NonPublic);
        callback.ShouldNotBeNull();
        callback!.Invoke(group, new object?[] { null });

        group.TryGet("expired", out _).ShouldBeFalse();
        group.Dispose();
    }

    [Fact]
    public void NumberGroupSummerCallback_RemovesExpiredEntries()
    {
        var interval = TimeSpan.FromMilliseconds(10);
        var group = new NumberGroupTimeSeriesSummer<int>(interval, samplesCount: 1, strategy: Strategy.Sum, deleteOverdueSamples: true);

        var summer = new NumberTimeSeriesSummer<int>(interval, maxSamplesCount: 1);
        var staleTimestamp = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(1));
        summer.AddNewData(staleTimestamp, 5);
        group.TimeSeries["expired"] = summer;

        var callback = typeof(BaseGroupNumberTimeSeriesSummer<int, NumberTimeSeriesSummer<int>, NumberGroupTimeSeriesSummer<int>>)
            .GetMethod("Callback", BindingFlags.Instance | BindingFlags.NonPublic);
        callback.ShouldNotBeNull();
        callback!.Invoke(group, new object?[] { null });

        group.TimeSeries.ContainsKey("expired").ShouldBeFalse();
        group.Dispose();
    }

    [Fact]
    public void BaseTimeSeriesSummer_StrategiesOperateCorrectly()
    {
        var interval = TimeSpan.FromMilliseconds(5);
        var now = DateTimeOffset.UtcNow;

        var sumSummer = new DummySummer(interval, maxSamplesCount: 8, Strategy.Sum);
        sumSummer.AddNewData(now, new DummySummerItem(1));
        sumSummer.AddNewData(now, new DummySummerItem(2));
        sumSummer.Sum().Value.ShouldBe(3);
        sumSummer.Increment();
        sumSummer.Decrement();

        var minSummer = new DummySummer(interval, maxSamplesCount: 8, Strategy.Min);
        minSummer.AddNewData(now, new DummySummerItem(5));
        minSummer.AddNewData(now, new DummySummerItem(2));
        minSummer.Sum().Value.ShouldBe(2);
        minSummer.Min()!.Value.ShouldBe(2);

        var maxSummer = new DummySummer(interval, maxSamplesCount: 8, Strategy.Max);
        maxSummer.AddNewData(now, new DummySummerItem(5));
        maxSummer.AddNewData(now, new DummySummerItem(7));
        maxSummer.Sum().Value.ShouldBe(7);
        maxSummer.Max()!.Value.ShouldBe(7);

        var replaceSummer = new DummySummer(interval, maxSamplesCount: 8, Strategy.Replace);
        replaceSummer.AddNewData(now, new DummySummerItem(1));
        replaceSummer.AddNewData(now, new DummySummerItem(9));
        replaceSummer.Sum().Value.ShouldBe(9);

        sumSummer.Merge(replaceSummer);
        sumSummer.DataCount.ShouldBeGreaterThan(0ul);

        var countBefore = sumSummer.DataCount;
        var lastBefore = sumSummer.LastDate;
        sumSummer.Resample(TimeSpan.FromMilliseconds(10), samplesCount: 4);
        sumSummer.DataCount.ShouldBe(countBefore);
        sumSummer.LastDate.ShouldBe(lastBefore.Round(TimeSpan.FromMilliseconds(10)));
    }

    private readonly struct DummySummerItem : ISummerItem<DummySummerItem>
    {
        public DummySummerItem(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public static DummySummerItem Zero => new(0);
        public static DummySummerItem One => new(1);

        public static DummySummerItem Min(DummySummerItem x, DummySummerItem y) => x.Value <= y.Value ? x : y;
        public static DummySummerItem Max(DummySummerItem x, DummySummerItem y) => x.Value >= y.Value ? x : y;

        public static DummySummerItem operator -(DummySummerItem value) => new(-value.Value);
        public static DummySummerItem operator +(DummySummerItem left, DummySummerItem right) => new(left.Value + right.Value);
    }

    private sealed class DummySummer : BaseTimeSeriesSummer<DummySummerItem, DummySummer>
    {
        public DummySummer(TimeSpan sampleInterval, int maxSamplesCount, Strategy strategy)
            : base(sampleInterval, maxSamplesCount, strategy)
        {
        }
    }
}
