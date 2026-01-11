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
    public static TimeSpan Round(this TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType)
    {
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
        var sign = ticks < 0 ? -1 : 1;
        var absTicks = ticks < 0 ? (ulong)(-ticks) : (ulong)ticks;
        var absInterval = (ulong)intervalTicks;

        var quotient = absTicks / absInterval;
        var remainder = absTicks % absInterval;

        if (remainder == 0)
        {
            return time;
        }

        if (roundingType is not (MidpointRounding.ToEven or MidpointRounding.AwayFromZero))
        {
            return RoundWithDecimal(time, roundingInterval, roundingType);
        }

        var roundUp = roundingType == MidpointRounding.AwayFromZero
            ? remainder * 2 >= absInterval
            : ShouldRoundToEven(quotient, remainder, absInterval);

        var rounded = (quotient + (roundUp ? 1UL : 0UL)) * absInterval;
        var roundedTicks = sign < 0 ? -(long)rounded : (long)rounded;
        return new TimeSpan(roundedTicks);
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

        return new DateTimeOffset(datetime, dateTimeOffset.Offset);
    }

    /// <summary>
    /// Rounds a <see cref="DateTimeOffset"/> to the specified interval and normalizes to UTC.
    /// </summary>
    /// <param name="dateTimeOffset">The date/time to round.</param>
    /// <param name="roundingInterval">The interval to round to.</param>
    public static DateTimeOffset RoundUtc(this DateTimeOffset dateTimeOffset, TimeSpan roundingInterval)
    {
        var datetime = dateTimeOffset.UtcDateTime.Round(roundingInterval);
        return new DateTimeOffset(datetime, TimeSpan.Zero);
    }

    private static TimeSpan RoundWithDecimal(TimeSpan time, TimeSpan roundingInterval, MidpointRounding roundingType)
    {
        var rounded = Math.Round(time.Ticks / (decimal)roundingInterval.Ticks, roundingType);
        return new TimeSpan(Convert.ToInt64(rounded) * roundingInterval.Ticks);
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
