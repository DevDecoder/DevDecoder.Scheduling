// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     Schedule options.
/// </summary>
[Flags]
public enum ScheduleOptions : byte
{
    /// <summary>
    ///     Default options.
    /// </summary>
    None = 0,

    /// <summary>
    ///     If set, then any errors thrown by the <see cref="IJob" /> will not cause the job to be disabled.
    /// </summary>
    IgnoreErrors = 1 << 1,

    /// <summary>
    ///     If set, the value passed into <see cref="ISchedule.Next" /> marks when the previous execution was due;
    ///     otherwise it is the <see cref="Instant" /> the previous execution was completed.
    /// </summary>
    /// <remarks>
    ///     In the event there has been no previous scheduled execution then this will be
    ///     <see cref="IClock.GetCurrentInstant()" />.
    /// </remarks>
    FromDue = 1 << 2,

    /// <summary>
    ///     Aligns any next due date to the next second.
    /// </summary>
    AlignSeconds = 1 << 3,

    /// <summary>
    ///     Aligns any next due date to the next minute.
    /// </summary>
    AlignMinutes = 1 << 4,

    /// <summary>
    ///     Aligns any next due date to the next hour.
    /// </summary>
    AlignHours = 1 << 5,

    /// <summary>
    ///     Aligns any next due date to the next day.
    /// </summary>
    AlignDays = 1 << 6,

    /// <summary>
    ///     If set, will not be passed a timer based cancellation token.
    /// </summary>
    LongRunning
}
