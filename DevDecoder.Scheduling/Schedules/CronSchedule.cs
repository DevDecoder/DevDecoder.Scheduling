// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Cronos;
using NodaTime;
using NodaTime.TimeZones;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Implements a <see cref="ISchedule">schedule</see> based on a <see cref="CronExpression">Cron expression</see>.
/// </summary>
public class CronSchedule : ISchedule
{
    /// <summary>
    ///     Creates a new <see cref="CronSchedule" />.
    /// </summary>
    /// <param name="expression">The cron expression, as a string.</param>
    /// <param name="cronFormat">The cron format.</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public CronSchedule(string expression, CronFormat cronFormat = CronFormat.Standard,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("expression")] string name = "")
        : this(CronExpression.Parse(expression, cronFormat), options, name)
    {
    }

    /// <summary>
    ///     Creates a new <see cref="CronSchedule" />.
    /// </summary>
    /// <param name="expression">The cron expression.</param>
    /// <param name="options">The options.</param>
    /// <param name="name">The optional name.</param>
    public CronSchedule(CronExpression expression,
        ScheduleOptions options = ScheduleOptions.None, [CallerArgumentExpression("expression")] string name = "")
    {
        Expression = expression;
        Name = name;
        Options = options;
    }

    /// <summary>
    /// The parsed <see cref="CronExpression"/>.
    /// </summary>
    public CronExpression Expression { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ScheduleOptions Options { get; }

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last)
    {
        // TODO shame we can't do this in NodaTime (see https://github.com/HangfireIO/Cronos/issues/55)
        var next = Expression.GetNextOccurrence(last.ToDateTimeUtc(), TimeZoneInfo.FindSystemTimeZoneById(last.Zone.Id));
        if (next is null) return null;
        var instant = Instant.FromDateTimeUtc(next.Value);
        return new ZonedDateTime(instant, last.Zone);
    }
}
