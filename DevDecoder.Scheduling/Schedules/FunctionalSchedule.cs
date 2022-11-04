// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using NodaTime;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Implements a <see cref="ISchedule">schedule</see> where the next execution's date and time is determined by the supplied
///     function.
/// </summary>
public class FunctionalSchedule : ISchedule
{
    /// <summary>
    ///     The function to get the next <see cref="Instant" />.
    /// </summary>
    private readonly Func<ZonedDateTime, ZonedDateTime?> _func;

    /// <summary>
    ///     Creates a new <see cref="FunctionalSchedule" />.
    /// </summary>
    /// <param name="func">The function to get the next due date.</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public FunctionalSchedule(Func<ZonedDateTime, ZonedDateTime?> func,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("func")] string name = "")
    {
        _func = func;
        Name = name;
        Options = options;
    }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public ScheduleOptions Options { get; set; }

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last) => _func(last);
}
