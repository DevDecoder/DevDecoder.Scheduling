// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using NodaTime;

namespace DevDecoder.Scheduling.Clocks;

/// <summary>
///     Singleton implementation of <see cref="IClock" /> which reads the current system time using
///     <see cref="Stopwatch.GetTimestamp" />.
///     It is recommended that for anything other than throwaway code, this is only referenced
///     in a single place in your code: where you provide a value to inject into the rest of
///     your application, which should only depend on the interface.
/// </summary>
/// <threadsafety>
///     This type has no state, and is thread-safe. See the thread safety section of the user guide for more
///     information.
/// </threadsafety>
public class FastClock : IPreciseClock
{
    /// <summary>
    ///     The instance of the clock.
    /// </summary>
    /// <remarks>
    ///     Note: if  is <see cref="IsAvailable" /> is <c>false</c>, this is the same as <see cref="StandardClock.Instance" />;
    ///     otherwise a
    /// </remarks>
    public static readonly IPreciseClock Instance;

    private static readonly Instant s_startedTime;
    private static readonly long s_startedTicks;

    private static readonly double s_swToTicks = 10000000D / Stopwatch.Frequency;

    /// <summary>
    ///     Static initializer.
    /// </summary>
    static FastClock()
    {
        s_startedTime = StandardClock.Instance.GetCurrentInstant();
        s_startedTicks = Stopwatch.GetTimestamp();
        IsAvailable = Stopwatch.IsHighResolution;
        Instance = IsAvailable ? new FastClock() : StandardClock.Instance;
    }

    /// <summary>
    ///     Ensure only singleton created.
    /// </summary>
    private FastClock()
    {
    }

    /// <summary>
    ///     <c>True</c> if the <see cref="FastClock" /> is available; otherwise <c>false</c>.
    /// </summary>
    /// <remarks>This is the effectively the same as <see cref="Stopwatch.IsHighResolution" />.</remarks>
    public static bool IsAvailable { get; }

    /// <inheritdoc />
    public Instant GetCurrentInstant() =>
        s_startedTime.PlusTicks((long)((Stopwatch.GetTimestamp() - s_startedTicks) * s_swToTicks));

    /// <inheritdoc />
    public ClockPrecision Precision => ClockPrecision.Fast;
}
