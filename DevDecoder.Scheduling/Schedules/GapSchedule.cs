// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using NodaTime;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Implements a <see cref="ISchedule">schedule</see> where the next execution continues a specific
///     <see cref="Duration" /> after the previous
///     execution,
/// </summary>
public class GapSchedule : ISchedule
{
    /// <summary>
    ///     Creates a new <see cref="GapSchedule" />.
    /// </summary>
    /// <param name="timeSpan">The timeSpan</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public GapSchedule(TimeSpan timeSpan,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("timeSpan")] string name = "")
        : this(Duration.FromTimeSpan(timeSpan), options, name)
    {
    }

    /// <summary>
    ///     Creates a new <see cref="GapSchedule" />.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public GapSchedule(Duration duration,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("duration")] string name = "")
    {
        Duration = duration < Duration.Zero ? Duration.Zero : duration;
        Name = name;
        Options = options;
    }

    /// <summary>
    ///     The gap between executions.
    /// </summary>
    public Duration Duration { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ScheduleOptions Options { get; }

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last) => last + Duration;
}
