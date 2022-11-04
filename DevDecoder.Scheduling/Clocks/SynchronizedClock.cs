// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using NodaTime;

namespace DevDecoder.Scheduling.Clocks;

/// <summary>
///     Singleton implementation of <see cref="IClock" /> which reads the current system time using
///     <see
///         href="https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtimepreciseasfiletime">
///         GetSystemTimePreciseAsFileTime
///     </see>
///     .
///     It is recommended that for anything other than throwaway code, this is only referenced
///     in a single place in your code: where you provide a value to inject into the rest of
///     your application, which should only depend on the interface.
/// </summary>
/// <threadsafety>
///     This type has no state, and is thread-safe. See the thread safety section of the user guide for more
///     information.
/// </threadsafety>
public class SynchronizedClock : IPreciseClock
{
    /// <summary>
    ///     The file time epoch, 12:00 A.M. January 1, 1601.
    /// </summary>
    public static readonly Instant FileTimeEpoch = Instant.FromUtc(1601, 1, 1, 0, 0);

    /// <summary>
    ///     Static initializer.
    /// </summary>
    static SynchronizedClock()
    {
        try
        {
            GetSystemTimePreciseAsFileTime(out _);
            IsAvailable = true;
            Instance = new SynchronizedClock();
        }
        catch
        {
            IsAvailable = false;
            Instance = StandardClock.Instance;
        }
    }

    /// <summary>
    ///     Ensure only singleton created.
    /// </summary>
    private SynchronizedClock() { }

    /// <summary>
    ///     The instance of the clock.
    /// </summary>
    /// <remarks>
    ///     Note: if  is <see cref="IsAvailable" /> is <c>false</c>, this is the same as <see cref="StandardClock.Instance" />;
    ///     otherwise a
    /// </remarks>
    public static IPreciseClock Instance { get; }

    /// <summary>
    ///     <c>True</c> if the <see cref="SynchronizedClock" /> is available; otherwise <c>false</c>.
    /// </summary>
    public static bool IsAvailable { get; }

    /// <inheritdoc />
    public ClockPrecision Precision => ClockPrecision.Synchronized;

    /// <inheritdoc />
    public Instant GetCurrentInstant()
    {
        GetSystemTimePreciseAsFileTime(out var time);
        return FileTimeEpoch.PlusTicks(time);
    }

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern void GetSystemTimePreciseAsFileTime(out long fileTime);
}
