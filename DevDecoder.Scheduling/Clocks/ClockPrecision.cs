// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

namespace DevDecoder.Scheduling.Clocks;

/// <summary>
///     Indicates the current clock precision.
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps" />
public enum ClockPrecision
{
    /// <summary>
    ///     The clock uses the
    ///     <see href="https://learn.microsoft.com/en-us/windows/win32/api/profileapi/nf-profileapi-queryperformancecounter">
    ///         Query Performance Counter
    ///     </see>
    ///     for fast, high-precision measurement. Usually accurate to less than 100 machine
    ///     cycles.
    /// </summary>
    /// <remarks>QPC is independent of, and isn't synchronized to, any external time reference.</remarks>
    Fast,

    /// <summary>
    ///     The clock uses <see cref="DateTime.UtcNow" />, which has a precision of 100ns.
    /// </summary>
    Standard,

    /// <summary>
    ///     The clock uses
    ///     <see
    ///         href="https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtimepreciseasfiletime">
    ///         GetSystemTimePreciseAsFileTime
    ///     </see>
    ///     fpr a synchronized accurate timestamp, to
    ///     greater than 1µs precision.
    /// </summary>
    /// <remarks>
    ///     GetSystemTimePreciseAsFileTime can be synchronized to an external time reference, e.g. by the Network Time
    ///     Protocol, such as, Coordinated Universal Time (UTC) for use in high-resolution time-of-day measurements.
    /// </remarks>
    Synchronized
}
