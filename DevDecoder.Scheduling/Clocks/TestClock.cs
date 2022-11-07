// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling.Clocks;

/// <summary>
///     Implements <see cref="IPreciseClock" /> for manual control during testing.
/// </summary>
public class TestClock : IPreciseClock
{
    /// <summary>
    ///     A clock that never runs as it always returns the latest possible date/time.
    /// </summary>
    public static readonly TestClock Never = new(_ => Instant.MaxValue);

    private readonly Func<Instant, Instant> _nextInstantFunc;
    private Instant _lastInstant;

    /// <summary>
    ///     Creates a new instance of the <see cref="TestClock" />.
    /// </summary>
    /// <param name="nextInstantFunc">A function that gets the next instant from the previous instant.</param>
    /// <param name="precision">The clock precision to report when queried, defaults to <see cref="ClockPrecision.Standard" />.</param>
    /// <param name="firstInstant">The first <see cref="Instant" /> to pass into <paramref name="nextInstantFunc" />.</param>
    public TestClock(Func<Instant, Instant> nextInstantFunc, ClockPrecision precision = ClockPrecision.Standard,
        Instant? firstInstant = null)
    {
        _nextInstantFunc = nextInstantFunc;
        Precision = precision;
        firstInstant ??= precision.GetInstant();
        _lastInstant = firstInstant.Value;
    }

    /// <summary>
    ///     The instance of the clock.
    /// </summary>
    /// <remarks>
    ///     This is an alias of <see cref="Never" />, for compatibility with the standard provided by the other
    ///     <see cref="IPreciseClock">clock implementations</see>.
    /// </remarks>
    public static TestClock Instance => Never;

    /// <inheritdoc />
    public Instant GetCurrentInstant() => _lastInstant = _nextInstantFunc(_lastInstant);

    /// <inheritdoc />
    public ClockPrecision Precision { get; set; }

    /// <summary>
    ///     Gets the actual current instant, based on the <paramref name="precision" /> specified, or the current
    ///     <see cref="Precision" />.
    /// </summary>
    /// <param name="precision">The optional clock precision, defaults to <see cref="Precision" />.</param>
    /// <returns></returns>
    public Instant GetActualCurrentInstant(ClockPrecision? precision = null) => (precision ?? Precision).GetInstant();

    /// <summary>
    ///     Creates a <see cref="TestClock" /> that always returns <paramref name="fixedInstant" />.
    /// </summary>
    /// <param name="fixedInstant">The fixed instant to return.</param>
    /// <param name="precision">The clock precision to report when queried, defaults to <see cref="ClockPrecision.Standard" />.</param>
    /// <returns>A new <see cref="TestClock" /> instance.</returns>
    public static TestClock Fixed(Instant fixedInstant, ClockPrecision precision = ClockPrecision.Standard)
        => new(_ => fixedInstant, precision);

    /// <summary>
    ///     Creates a <see cref="TestClock" /> that increases by <paramref name="interval" />, starting with
    ///     <paramref name="firstInstant" />, each time <see cref="GetCurrentInstant" /> is called.
    /// </summary>
    /// <param name="firstInstant">The first instant to return.</param>
    /// <param name="interval">The duration to increase by each call, defaults to one second.</param>
    /// <param name="precision">The clock precision to report when queried, defaults to <see cref="ClockPrecision.Standard" />.</param>
    /// <returns>A new <see cref="TestClock" /> instance.</returns>
    public static TestClock From(Instant? firstInstant = null, Duration? interval = null,
        ClockPrecision precision = ClockPrecision.Standard)
    {
        firstInstant ??= precision.GetInstant();
        interval ??= Duration.FromSeconds(1);
        var d = interval.Value;
        return new TestClock(i => i + d, precision, firstInstant.Value - d);
    }
}
