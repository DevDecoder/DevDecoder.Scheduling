// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling.Clocks;

/// <summary>
///     Singleton implementation of <see cref="IPreciseClock" /> which reads the current system time using
///     <see cref="DateTime.UtcNow" />.
///     It is recommended that for anything other than throwaway code, this is only referenced
///     in a single place in your code: where you provide a value to inject into the rest of
///     your application, which should only depend on the interface.
/// </summary>
/// <remarks>
///     Note this is functionally identical to <see cref="SystemClock" /> from NodaTime, except it exposes a
///     <see cref="Precision" /> property.
/// </remarks>
/// <threadsafety>
///     This type has no state, and is thread-safe. See the thread safety section of the user guide for more
///     information.
/// </threadsafety>
public sealed class StandardClock : IPreciseClock
{
    /// <summary>
    ///     Ensure only singleton created.
    /// </summary>
    private StandardClock()
    {
    }

    /// <summary>
    ///     The singleton instance of <see cref="SystemClock" />.
    /// </summary>
    /// <value>The singleton instance of <see cref="SystemClock" />.</value>
    public static IPreciseClock Instance { get; } = new StandardClock();

    /// <inheritdoc />
    public Instant GetCurrentInstant() => NodaConstants.BclEpoch.PlusTicks(DateTime.UtcNow.Ticks);

    /// <inheritdoc />
    public ClockPrecision Precision => ClockPrecision.Standard;
}
