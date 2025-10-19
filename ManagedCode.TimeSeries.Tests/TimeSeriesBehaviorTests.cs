using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ManagedCode.TimeSeries.Accumulators;
using ManagedCode.TimeSeries.Summers;
using ManagedCode.TimeSeries.Abstractions;
using ManagedCode.TimeSeries.Extensions;
using ManagedCode.TimeSeries.Orleans;
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

        accumulator.Samples.Count.Should().Be(3);
        accumulator.Samples.Keys.Should().Equal(
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

        accumulator.Samples.Count.Should().Be(4);
        accumulator.DeleteOverdueSamples();
        accumulator.Samples.Count.Should().BeLessThanOrEqualTo(4);
        accumulator.Samples.Keys.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Accumulator_ResetSamples_ClearsState()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(1), maxSamplesCount: 4);
        var now = DateTimeOffset.UtcNow;
        accumulator.AddNewData(now, 1);
        accumulator.InternalStorage.Should().NotBeEmpty();

        accumulator.ForceReset();

        accumulator.Samples.Should().BeEmpty();
        accumulator.Start.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        accumulator.End.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Accumulator_IsOverflowPropertyAccessible()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(1), maxSamplesCount: 1);
        accumulator.AddNewData(DateTimeOffset.UtcNow, 42);
        accumulator.AddNewData(DateTimeOffset.UtcNow.AddMilliseconds(1), 43);
        accumulator.ForceSetMaxSamples(-1);
        var overflow = accumulator.IsOverflow;
        overflow.Should().BeFalse();
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

        rebase.Samples.Count.Should().Be(4);
        rebase.DataCount.Should().Be(4);
        rebase.Samples.Keys.Should().Equal(
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

        target.DataCount.Should().Be(3);
        target.Samples.Count.Should().Be(3);
    }

    [Fact]
    public void Accumulator_OperatorPlus_ProducesMergedSnapshot()
    {
        var left = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var right = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);

        left.AddNewData(DateTimeOffset.UnixEpoch, 1);
        right.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(5), 2);

        var merged = left + right;
        merged.Samples.Count.Should().Be(right.Samples.Count);
        merged.DataCount.Should().Be(right.DataCount);
    }

    [Fact]
    public void Accumulator_CheckedAddition_ProducesMergedSnapshot()
    {
        var left = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var right = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);

        left.AddNewData(DateTimeOffset.UnixEpoch, 1);
        right.AddNewData(DateTimeOffset.UnixEpoch.AddMilliseconds(5), 2);

        var merged = checked(left + right);
        merged.Samples.Count.Should().Be(right.Samples.Count);
        merged.DataCount.Should().Be(right.DataCount);
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

        left.Samples.Count.Should().Be(1);
        var values = left.Samples.First().Value;
        values.Should().ContainKeys("alpha", "beta", "gamma");
    }

    [Fact]
    public void StringAccumulator_ResampleSmallerIntervalThrows()
    {
        var accumulator = new StringSetAccumulator(TimeSpan.FromSeconds(1));
        accumulator.AddNewData(DateTimeOffset.UtcNow, "value");

        Action act = () => accumulator.Resample(TimeSpan.FromMilliseconds(100), 4);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GroupAccumulator_TryGetAndRemove_BehavesAsExpected()
    {
        var group = new IntGroupTimeSeriesAccumulator(
            sampleInterval: TimeSpan.FromMilliseconds(5),
            maxSamplesCount: 32,
            deleteOverdueSamples: false);

        group.AddNewData("alpha", DateTimeOffset.UtcNow, 1);

        group.TryGet("alpha", out var first).Should().BeTrue();
        first.Should().NotBeNull();
        group.Remove("alpha").Should().BeTrue();
        group.TryGet("alpha", out _).Should().BeFalse();
    }

    [Fact]
    public void NumberGroupSummer_NoData_ReturnsZeros()
    {
        var group = new NumberGroupTimeSeriesSummer<int>(
            sampleInterval: TimeSpan.FromSeconds(1),
            samplesCount: 16,
            strategy: Strategy.Sum,
            deleteOverdueSamples: false);

        group.Sum().Should().Be(0);
        group.Average().Should().Be(0);
        group.Min().Should().Be(0);
        group.Max().Should().Be(0);
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

        group.Sum().Should().Be(0);
        group.Max().Should().Be(0);
        group.Min().Should().Be(0);
    }

    [Fact]
    public void NumberSummer_DecrementProducesNegativeValues()
    {
        var summer = new NumberTimeSeriesSummer<int>(TimeSpan.FromMilliseconds(10));
        summer.Decrement();
        summer.Sum().Should().Be(-1);
        summer.Min().Should().Be(-1);
        summer.Max().Should().Be(-1);
    }

    [Fact]
    public void NumberSummer_EmptyFactoryCreatesInstance()
    {
        var empty = NumberTimeSeriesSummer<int>.Empty(TimeSpan.FromSeconds(2), maxSamplesCount: 5);
        empty.Should().NotBeNull();
        empty.Samples.Should().BeEmpty();
    }

    [Fact]
    public void OrleansAccumulatorConverter_RoundTripsEmptySeries()
    {
        var converter = new IntTimeSeriesAccumulatorConverter<int>();
        var source = new IntTimeSeriesAccumulator(TimeSpan.FromSeconds(1), maxSamplesCount: 4);

        var surrogate = converter.ConvertToSurrogate(source);
        surrogate.Samples.Should().BeEmpty();

        var restored = converter.ConvertFromSurrogate(in surrogate);
        restored.Samples.Should().BeEmpty();
        restored.DataCount.Should().Be(0);
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
        floatRoundTripped.Should().Be(floatOriginal);

        var doubleSummer = new DoubleTimeSeriesSummer(TimeSpan.FromMilliseconds(10), maxSamplesCount: 8, Strategy.Sum);
        doubleSummer.AddNewData(DateTimeOffset.UnixEpoch, 3.14);
        var doubleConverter = new DoubleTimeSeriesSummerConverter<double>();
        var doubleSurrogate = doubleConverter.ConvertToSurrogate(doubleSummer);
        var doubleRestored = doubleConverter.ConvertFromSurrogate(doubleSurrogate);
        doubleRestored.Sum().Should().Be(doubleSummer.Sum());
    }

    [Fact]
    public void OrleansSummerConverter_RoundTripsEmptySeries()
    {
        var converter = new IntTimeSeriesSummerConverter<int>();
        var summer = new IntTimeSeriesSummer(TimeSpan.FromMilliseconds(5), maxSamplesCount: 4, strategy: Strategy.Sum);

        var surrogate = converter.ConvertToSurrogate(summer);
        surrogate.Samples.Should().BeEmpty();

        var restored = converter.ConvertFromSurrogate(in surrogate);
        restored.Samples.Should().BeEmpty();
        restored.Sum().Should().Be(0);
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

        accumulator.Start.Should().Be(newStart);
        accumulator.End.Should().Be(newEnd);
    }

    [Fact]
    public void Accumulator_SamplesExposeDictionarySemantics()
    {
        var accumulator = new TestAccumulator(TimeSpan.FromMilliseconds(5), maxSamplesCount: 8);
        var timestamp = DateTimeOffset.UnixEpoch;
        accumulator.AddNewData(timestamp, 99);

        accumulator.Samples.ContainsKey(timestamp).Should().BeTrue();
        accumulator.Samples.TryGetValue(timestamp, out var queue).Should().BeTrue();
        queue.Should().NotBeNull();

        var enumerator = ((IEnumerable)accumulator.Samples).GetEnumerator();
        enumerator.MoveNext().Should().BeTrue();
    }

    [Fact]
    public void Accumulator_CanBeConstructedWithExplicitRange()
    {
        var start = DateTimeOffset.UnixEpoch.AddMinutes(1);
        var end = start.AddMinutes(5);
        var last = end.AddSeconds(30);

        var accumulator = new TestAccumulator(TimeSpan.FromSeconds(1), 16, start, end, last);
        accumulator.Start.Should().Be(start.Round(TimeSpan.FromSeconds(1)));
        accumulator.End.Should().Be(end.Round(TimeSpan.FromSeconds(1)));
        accumulator.LastDate.Should().Be(last);
    }

    [Fact]
    public void AtomicDateTimeOffset_DefaultReadReturnsDefault()
    {
        var assembly = typeof(BaseTimeSeries<,,>).Assembly;
        var atomicType = assembly.GetType("ManagedCode.TimeSeries.Abstractions.AtomicDateTimeOffset", throwOnError: true)!;
        var atomic = Activator.CreateInstance(atomicType);
        var readMethod = atomicType.GetMethod("Read", BindingFlags.Public | BindingFlags.Instance)!;
        var result = (DateTimeOffset)readMethod.Invoke(atomic, Array.Empty<object>())!;
        result.Should().Be(default);
    }
}
