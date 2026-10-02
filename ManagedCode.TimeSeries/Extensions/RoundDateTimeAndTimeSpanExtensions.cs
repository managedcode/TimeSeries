namespace ManagedCode.TimeSeries.Extensions;

/// <summary>
/// Rounding helpers for time values.
/// </summary>
public static class RoundDateTimeAndTimeSpanExtensions
{
    /// <summary>
    /// Rounds a <see cref="TimeSpan"/> to the specified interval.
    /// </summary>
    /// <param name="time">The time span to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    /// <param name="roundingType">The midpoint rounding mode.</param>
    /// <exception cref="ArgumentOutOfRangeException">The interval is not positive or the rounding mode is not supported.</exception>
    /// <exception cref="OverflowException">The rounded result cannot be represented by a <see cref="TimeSpan"/>.</exception>
    public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType)
    {
        if (roundingType is not (MidpointRounding.ToEven or
            MidpointRounding.AwayFromZero or
            MidpointRounding.ToZero or
            MidpointRounding.ToNegativeInfinity or
            MidpointRounding.ToPositiveInfinity))
        {
            throw new ArgumentOutOfRangeException(nameof(roundingType), roundingType, "Unsupported midpoint rounding mode.");
        }

        if (roundingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(roundingInterval), "Rounding interval must be positive.");
        }

        var intervalTicks = roundingInterval.Ticks;
        if (intervalTicks == 1 || time.Ticks == 0)
        {
            return time;
        }

        var ticks = time.Ticks;
        var negative = ticks < 0;
        var absTicks = negative ? (ulong)(-(ticks + 1)) + 1UL : (ulong)ticks;
        var absInterval = (ulong)intervalTicks;

        var quotient = absTicks / absInterval;
        var remainder = absTicks % absInterval;

        if (remainder == 0)
        {
            return time;
        }

        var roundUp = ShouldIncrement(quotient, remainder, absInterval, negative, roundingType);
        var roundedMagnitude = (quotient + (roundUp ? 1UL : 0UL)) * absInterval;

        if (negative)
        {
            const ulong MinMagnitude = 1UL << 63;
            if (roundedMagnitude > MinMagnitude)
            {
                throw new OverflowException("The rounded time span is outside the representable range.");
            }

            return new TimeSpan(roundedMagnitude == MinMagnitude ? long.MinValue : -(long)roundedMagnitude);
        }

        if (roundedMagnitude > long.MaxValue)
        {
            throw new OverflowException("The rounded time span is outside the representable range.");
        }

        return new TimeSpan((long)roundedMagnitude);
    }

    /// <summary>
    /// Rounds a <see cref="TimeSpan"/> to the specified interval using midpoint-to-even.
    /// </summary>
    /// <param name="time">The time span to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval)
    {
        return Round(time, roundingInterval, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Rounds a <see cref="DateTime"/> to the specified interval.
    /// </summary>
    /// <param name="datetime">The date/time to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    public static DateTime Round(this DateTime datetime, TimeSpan roundingInterval)
    {
        return new DateTime((datetime - DateTime.MinValue).Round(roundingInterval).Ticks, datetime.Kind);
    }

    /// <summary>
    /// Rounds a <see cref="DateTimeOffset"/> to the specified interval, preserving the offset.
    /// </summary>
    /// <param name="dateTimeOffset">The date/time to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    public static DateTimeOffset Round(this DateTimeOffset dateTimeOffset, TimeSpan roundingInterval)
    {
        var datetime = dateTimeOffset.UtcDateTime.Round(roundingInterval);

        return new DateTimeOffset(datetime.Ticks, TimeSpan.Zero).ToOffset(dateTimeOffset.Offset);
    }

    /// <summary>
    /// Rounds a <see cref="DateTimeOffset"/> to the specified interval and normalizes to UTC.
    /// </summary>
    /// <param name="dateTimeOffset">The date/time to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    public static DateTimeOffset RoundUtc(this DateTimeOffset dateTimeOffset, TimeSpan roundingInterval)
    {
        var datetime = dateTimeOffset.UtcDateTime.Round(roundingInterval);
        return new DateTimeOffset(datetime.Ticks, TimeSpan.Zero);
    }

    private static bool ShouldIncrement(
        ulong quotient,
        ulong remainder,
        ulong interval,
        bool negative,
        MidpointRounding roundingType)
    {
        return roundingType switch
        {
            MidpointRounding.ToEven => ShouldRoundToEven(quotient, remainder, interval),
            MidpointRounding.AwayFromZero => remainder * 2UL >= interval,
            MidpointRounding.ToZero => false,
            MidpointRounding.ToNegativeInfinity => negative,
            MidpointRounding.ToPositiveInfinity => !negative,
            _ => throw new ArgumentOutOfRangeException(nameof(roundingType), roundingType, "Unsupported midpoint rounding mode.")
        };
    }

    private static bool ShouldRoundToEven(ulong quotient, ulong remainder, ulong interval)
    {
        var doubleRemainder = remainder * 2;
        if (doubleRemainder < interval)
        {
            return false;
        }

        if (doubleRemainder > interval)
        {
            return true;
        }

        return (quotient & 1UL) == 1UL;
    }
}
