// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using NodaTime;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Implements a <see cref="ISchedule">schedule</see> which executes once, at the specified <see cref="ZonedDateTime" />.
/// </summary>
public class OneOffSchedule : ISchedule
{
    /// <summary>
    ///     Creates a new <see cref="OneOffSchedule" />.
    /// </summary>
    /// <param name="zonedDateTime">The zoned date and time to execute.</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public OneOffSchedule(ZonedDateTime zonedDateTime,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("zonedDateTime")] string name = "")
    {
        ZonedDateTime = zonedDateTime;
        Name = name;
        Options = options;
    }

    /// <summary>
    ///     The instant the execution should occur, if any; otherwise <c>null</c>.
    /// </summary>
    public ZonedDateTime? ZonedDateTime { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ScheduleOptions Options { get; }

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last) =>
        ZonedDateTime is null
            ? null
            : NodaTime.ZonedDateTime.Comparer.Instant.Compare(ZonedDateTime.Value, last) > 0
                ? ZonedDateTime
                : null;
}
