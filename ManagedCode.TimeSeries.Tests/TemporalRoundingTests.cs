using System.Numerics;
using ManagedCode.TimeSeries.Extensions;
using Shouldly;
using Xunit;

namespace ManagedCode.TimeSeries.Tests;

public class TemporalRoundingTimeSpanTests
{
    public static IEnumerable<object[]> BoundaryCases()
    {
        var values = new HashSet<long>
        {
            long.MinValue, long.MinValue + 1, long.MinValue + 2, long.MinValue + 3,
            -11, -10, -9, -7, -5, -3, -2, -1, 0, 1, 2, 3, 5, 7, 9, 10, 11,
            long.MaxValue - 3, long.MaxValue - 2, long.MaxValue - 1, long.MaxValue
        };
        var intervals = new[] { 1L, 2L, 3L, 4L, 10L, 1_000_003L, long.MaxValue - 1, long.MaxValue };
        var modes = Enum.GetValues<MidpointRounding>();

        foreach (var ticks in values)
        {
            foreach (var interval in intervals)
            {
                foreach (var mode in modes)
                {
                    yield return [ticks, interval, mode];
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(BoundaryCases))]
    public void Round_MatchesIndependentIntegerOracle(long ticks, long intervalTicks, MidpointRounding mode)
    {
        var time = TimeSpan.FromTicks(ticks);
        var interval = TimeSpan.FromTicks(intervalTicks);
        var expected = RoundTicks(ticks, intervalTicks, mode);

        if (expected is null)
        {
            Should.Throw<OverflowException>(() => time.Round(interval, mode));
            return;
        }

        time.Round(interval, mode).Ticks.ShouldBe(expected.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3)]
    public void Round_RejectsNonPositiveIntervalBeforeExactValueShortcut(long intervalTicks)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            TimeSpan.Zero.Round(TimeSpan.FromTicks(intervalTicks), MidpointRounding.ToEven));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 2)]
    [InlineData(1, 1)]
    [InlineData(-1, 1)]
    public void Round_RejectsUnknownModeBeforeZeroOrUnitIntervalShortcut(long ticks, long intervalTicks)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            TimeSpan.FromTicks(ticks).Round(TimeSpan.FromTicks(intervalTicks), (MidpointRounding)99));
    }

    private static long? RoundTicks(long ticks, long intervalTicks, MidpointRounding mode)
    {
        var signedTicks = new BigInteger(ticks);
        var interval = new BigInteger(intervalTicks);
        var negative = signedTicks.Sign < 0;
        var quotient = BigInteger.DivRem(BigInteger.Abs(signedTicks), interval, out var remainder);
        var roundUp = ShouldIncrement(quotient, remainder, interval, negative, mode);
        var roundedMagnitude = (quotient + (roundUp ? BigInteger.One : BigInteger.Zero)) * interval;
        var result = negative ? -roundedMagnitude : roundedMagnitude;

        if (result < long.MinValue || result > long.MaxValue)
        {
            return null;
        }

        return (long)result;
    }

    private static bool ShouldIncrement(
        BigInteger quotient,
        BigInteger remainder,
        BigInteger interval,
        bool negative,
        MidpointRounding mode)
    {
        if (remainder.IsZero)
        {
            return false;
        }

        return mode switch
        {
            MidpointRounding.ToEven => remainder * 2 > interval ||
                                      (remainder * 2 == interval && !quotient.IsEven),
            MidpointRounding.AwayFromZero => remainder * 2 >= interval,
            MidpointRounding.ToZero => false,
            MidpointRounding.ToNegativeInfinity => negative,
            MidpointRounding.ToPositiveInfinity => !negative,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
    }
}

public class TemporalRoundingDateTimeTests
{
    [Theory]
    [InlineData(14)]
    [InlineData(-11)]
    [InlineData(0)]
    public void DateTimeOffsetRound_PreservesOffsetAndRoundsUtcInstant(int offsetHours)
    {
        var offset = TimeSpan.FromHours(offsetHours);
        var interval = TimeSpan.FromTicks(2);
        var utcBaseTicks = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        var lowerBucket = utcBaseTicks / interval.Ticks;
        var utcTicks = (lowerBucket + 1) * interval.Ticks + 1;
        var utc = new DateTimeOffset(new DateTime(utcTicks, DateTimeKind.Utc));
        var input = utc.ToOffset(offset);
        var expectedTicks = RoundTicks(utcTicks, interval.Ticks, MidpointRounding.ToEven);
        expectedTicks.ShouldNotBeNull();
        var expectedUtc = new DateTimeOffset(new DateTime(expectedTicks.Value, DateTimeKind.Unspecified), TimeSpan.Zero);

        var rounded = input.Round(interval);
        rounded.Offset.ShouldBe(offset);
        rounded.UtcTicks.ShouldBe(expectedUtc.UtcTicks);

        var roundedUtc = input.RoundUtc(interval);
        roundedUtc.Offset.ShouldBe(TimeSpan.Zero);
        roundedUtc.UtcTicks.ShouldBe(expectedUtc.UtcTicks);
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void DateTimeRound_PreservesKindAtTemporalEdges(DateTimeKind kind)
    {
        var interval = TimeSpan.FromTicks(3);
        var candidates = new[]
        {
            DateTime.MinValue.Ticks + 1,
            DateTime.MinValue.Ticks + 2,
            new DateTime(2024, 1, 1).Ticks + 1,
            DateTime.MaxValue.Ticks - 2,
            DateTime.MaxValue.Ticks - 1,
            DateTime.MaxValue.Ticks
        };

        foreach (var ticks in candidates)
        {
            var input = new DateTime(ticks, kind);
            var expectedTicks = RoundTicks(ticks, interval.Ticks, MidpointRounding.ToEven);
            if (expectedTicks is null || expectedTicks < DateTime.MinValue.Ticks || expectedTicks > DateTime.MaxValue.Ticks)
            {
                Should.Throw<ArgumentOutOfRangeException>(() => input.Round(interval));
                continue;
            }

            var result = input.Round(interval);
            result.Ticks.ShouldBe(expectedTicks.Value);
            result.Kind.ShouldBe(kind);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DateTimeOffsetRound_RejectsInvalidInterval(long intervalTicks)
    {
        var input = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(5));

        Should.Throw<ArgumentOutOfRangeException>(() => input.Round(TimeSpan.FromTicks(intervalTicks)));
        Should.Throw<ArgumentOutOfRangeException>(() => input.RoundUtc(TimeSpan.FromTicks(intervalTicks)));
    }

    private static long? RoundTicks(long ticks, long intervalTicks, MidpointRounding mode)
    {
        var value = new BigInteger(ticks);
        var interval = new BigInteger(intervalTicks);
        var quotient = BigInteger.DivRem(value, interval, out var remainder);
        var absoluteRemainder = BigInteger.Abs(remainder);
        var negative = value.Sign < 0;
        var roundUp = absoluteRemainder * 2 > interval ||
                      (absoluteRemainder * 2 == interval && !quotient.IsEven);
        var rounded = (quotient + (roundUp ? (negative ? -BigInteger.One : BigInteger.One) : BigInteger.Zero)) * interval;

        return rounded < long.MinValue || rounded > long.MaxValue ? null : (long)rounded;
    }
}
