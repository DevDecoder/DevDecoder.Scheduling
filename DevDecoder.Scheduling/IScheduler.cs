// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using DevDecoder.Scheduling.Jobs;
using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     Interface for a scheduler that can run <see cref="IJob">jobs</see> on a <see cref="ISchedule">schedule</see>.
/// </summary>
public interface IScheduler
{
    /// <summary>
    ///     The clock used by the scheduler.
    /// </summary>
    IPreciseClock Clock { get; }

    /// <summary>
    ///     The date/time zone provider.
    /// </summary>
    IDateTimeZoneProvider DateTimeZoneProvider { get; }

    /// <summary>
    /// The current date/time zone.
    /// </summary>
    DateTimeZone DateTimeZone { get; set; }

    /// <summary>
    ///     The maximum amount of time to let a job run for before cancelling.
    /// </summary>
    Duration MaximumExecutionDuration { get; set; }

    /// <summary>
    /// Get's the current <see cref="ZonedDateTime"/>.
    /// </summary>
    ZonedDateTime GetCurrentZonedDateTime() => Clock.GetCurrentInstant().InZone(DateTimeZone);

    /// <summary>
    ///     Tries to remove the specified <see cref="IScheduledJob">scheduled job</see>
    /// </summary>
    bool TryRemove(IScheduledJob job);

    /// <summary>
    ///     Adds a job to run on a schedule.
    /// </summary>
    /// <param name="job"></param>
    /// <param name="schedule">The schedule</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    IScheduledJob Add(IJob job, ISchedule schedule);

}
