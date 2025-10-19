using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace ManagedCode.TimeSeries.Tests.Assertions;

internal static class ShouldlyExtensions
{
    public static void ShouldHaveCount<T>(this IEnumerable<T> source, int expected)
    {
        source.Count().ShouldBe(expected);
    }

    public static void ShouldHaveCount<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, int expected)
    {
        source.Count.ShouldBe(expected);
    }

    public static void ShouldBeInAscendingOrder<T>(this IEnumerable<T> source) where T : IComparable<T>
    {
        var values = source as T[] ?? source.ToArray();
        var ordered = values.OrderBy(static value => value).ToArray();
        values.ShouldBe(ordered);
    }

    public static void ShouldContainKeys<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> source, params TKey[] keys)
    {
        foreach (var key in keys)
        {
            source.ContainsKey(key).ShouldBeTrue();
        }
    }

    public static void ShouldBeCloseTo(this DateTimeOffset actual, DateTimeOffset expected, TimeSpan tolerance)
    {
        (actual - expected).Duration().ShouldBeLessThanOrEqualTo(tolerance);
    }

    public static void ShouldSequenceEqual<T>(this IEnumerable<T> actual, params T[] expected)
    {
        actual.ToArray().ShouldBe(expected);
    }
}
