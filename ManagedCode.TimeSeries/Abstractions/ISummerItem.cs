using System.Numerics;

namespace ManagedCode.TimeSeries.Abstractions;

public interface ISummerItem<TSelf> :
    IUnaryNegationOperators<TSelf, TSelf>,
    IAdditionOperators<TSelf, TSelf, TSelf>
    where TSelf : ISummerItem<TSelf>
{
    /// <summary>Gets the radix, or base, for the type.</summary>
    static abstract TSelf Zero { get; }

    /// <summary>Gets the value <c>1</c> for the type.</summary>
    static abstract TSelf One { get; }

    /// <summary>Compares two values to compute which is lesser.</summary>
    /// <param name="x">The value to compare with <paramref name="y" />.</param>
    /// <param name="y">The value to compare with <paramref name="x" />.</param>
    /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
    static abstract TSelf Min(TSelf x, TSelf y);

    /// <summary>Compares two values to compute which is greater.</summary>
    /// <param name="x">The value to compare with <paramref name="y" />.</param>
    /// <param name="y">The value to compare with <paramref name="x" />.</param>
    /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
    static abstract TSelf Max(TSelf x, TSelf y);
}