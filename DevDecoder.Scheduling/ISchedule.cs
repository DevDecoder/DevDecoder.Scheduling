// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     A job schedule.
/// </summary>
public interface ISchedule
{
    /// <summary>
    ///     An optional name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the options.
    /// </summary>
    ScheduleOptions Options { get; }

    /// <summary>
    ///     Gets the next scheduled event.
    /// </summary>
    /// <param name="scheduler">The scheduler requesting the next date/time.</param>
    /// <param name="last">The last <see cref="ZonedDateTime" /> the job was completed, or started (see <see cref="Options" />), if any; otherwise it is the current date and time.</param>
    /// <returns>The next <see cref="ZonedDateTime" /> in the schedule, or <c>null</c> for stopped.</returns>
    ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last);
}
