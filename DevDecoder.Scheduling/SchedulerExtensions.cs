// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using DevDecoder.Scheduling.Jobs;
using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     Useful extension methods.
/// </summary>
public static class SchedulerExtensions
{
    private static readonly Duration s_oneSecond = Duration.FromSeconds(1);
    private static readonly Duration s_oneMinute = Duration.FromMinutes(1);
    private static readonly Duration s_oneHour = Duration.FromHours(1);
    private static readonly Duration s_oneDay = Duration.FromDays(1);

    /// <summary>
    ///     Rounds the <paramref name="localDateTime" /> up based on the <paramref name="duration" />.
    /// </summary>
    /// <param name="localDateTime">The <see cref="LocalDateTime" /> to round up.</param>
    /// <param name="duration">The duration to round to.</param>
    /// <returns></returns>
    public static LocalTime RoundUpToDuration(this LocalTime localDateTime, Duration duration)
    {
        if (duration <= Duration.Zero)
        {
            return localDateTime;
        }

        var ticksInDuration = duration.BclCompatibleTicks;
        var ticksInDay = localDateTime.TickOfDay;
        var ticksAfterRounding = ticksInDay % ticksInDuration;
        if (ticksAfterRounding == 0)
        {
            return localDateTime;
        }

        // Create period to add ticks to get to next rounding.
        var period = Period.FromTicks(ticksInDuration - ticksAfterRounding);
        return localDateTime.Plus(period);
    }

    /// <summary>
    ///     Rounds the <paramref name="offsetDateTime" /> up based on the <paramref name="duration" />.
    /// </summary>
    /// <param name="offsetDateTime">The <see cref="OffsetDateTime" /> to round up.</param>
    /// <param name="duration">The duration to round to.</param>
    /// <returns></returns>
    public static OffsetDateTime RoundUpToDuration(this OffsetDateTime offsetDateTime, Duration duration)
    {
        if (duration <= Duration.Zero)
        {
            return offsetDateTime;
        }

        var result = offsetDateTime.With(t => RoundUpToDuration(t, duration));
        if (OffsetDateTime.Comparer.Instant.Compare(offsetDateTime, result) > 0)
        {
            result = result.Plus(s_oneDay);
        }

        return result;
    }

    /// <summary>
    ///     Rounds the <paramref name="zonedDateTime" /> up based on the <paramref name="duration" />.
    /// </summary>
    /// <param name="zonedDateTime">The <see cref="ZonedDateTime" /> to round up.</param>
    /// <param name="duration">The duration to round to.</param>
    /// <returns></returns>
    public static ZonedDateTime RoundUpToDuration(this ZonedDateTime zonedDateTime, Duration duration)
    {
        if (duration <= Duration.Zero)
        {
            return zonedDateTime;
        }

        var odt = zonedDateTime.ToOffsetDateTime().RoundUpToDuration(duration);
        return odt.InZone(zonedDateTime.Zone);
    }

    /// <summary>
    ///     Applies the <paramref name="options" /> to the <paramref name="zonedDateTime" /> to align the result, where
    ///     necessary.
    /// </summary>
    /// <param name="zonedDateTime">The <see cref="ZonedDateTime" /> to align.</param>
    /// <param name="options">The alignment options.</param>
    /// <returns>An aligned <see cref="ZonedDateTime" />.</returns>
    public static ZonedDateTime? ApplyOptions(this ZonedDateTime? zonedDateTime, ScheduleOptions options)
    {
        if (zonedDateTime is null || options <= ScheduleOptions.FromDue)
        {
            return zonedDateTime;
        }

        // Round up due time.
        if (options.HasFlag(ScheduleOptions.AlignDays))
        {
            return zonedDateTime.Value.RoundUpToDuration(s_oneDay);
        }

        if (options.HasFlag(ScheduleOptions.AlignHours))
        {
            return zonedDateTime.Value.RoundUpToDuration(s_oneHour);
        }

        if (options.HasFlag(ScheduleOptions.AlignMinutes))
        {
            return zonedDateTime.Value.RoundUpToDuration(s_oneMinute);
        }

        return options.HasFlag(ScheduleOptions.AlignSeconds)
            ? zonedDateTime.Value.RoundUpToDuration(s_oneSecond)
            : zonedDateTime;
    }

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="action">The action.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob Add(this IScheduler scheduler, Action action, ISchedule schedule,
        [CallerArgumentExpression("action")] string name = "")
        => scheduler.Add(SimpleJob.Create(action, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action&lt;T1>" />.
    /// </summary
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="action">The action.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob Add(this IScheduler scheduler, Action<IJobState> action, ISchedule schedule,
        [CallerArgumentExpression("action")] string name = "")
        => scheduler.Add(SimpleJob.Create(action, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action&lt;T1, T2>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="action">The action.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob Add(this IScheduler scheduler, Action<IJobState, CancellationToken> action,
        ISchedule schedule,
        [CallerArgumentExpression("action")] string name = "")
        => scheduler.Add(SimpleJob.Create(action, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="func">The function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob Add<TResult>(this IScheduler scheduler, Func<TResult> func, ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.Create(func, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="func">The function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob Add<TResult>(this IScheduler scheduler, Func<IJobState, TResult> func,
        ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.Create(func, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="func">The function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob Add<TResult>(this IScheduler scheduler,
        Func<IJobState, CancellationToken, TResult> func, ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.Create(func, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="action">The async action.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob AddAsync(this IScheduler scheduler, Func<Task> action, ISchedule schedule,
        [CallerArgumentExpression("action")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(action, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="action">The async action.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob AddAsync(this IScheduler scheduler, Func<IJobState, Task> action, ISchedule schedule,
        [CallerArgumentExpression("action")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(action, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="func">The async function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler, Func<Task<TResult>> func,
        ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(func, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="func">The async function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler, Func<IJobState, Task<TResult>> func,
        ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(func, name), schedule);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="func">The async function.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler,
        Func<IJobState, CancellationToken, Task<TResult>> func, ISchedule schedule,
        [CallerArgumentExpression("func")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(func, name), schedule);


    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="ExecuteDelegate" />.
    /// </summary>
    /// <param name="scheduler">The scheduler.</param>
    /// <param name="d">The delegate.</param>
    /// <param name="schedule">The schedule</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="d" />.</param>
    /// <returns>
    ///     A <see cref="IScheduledJob">scheduled job</see>, allowing for manual execution of the added
    ///     <see cref="IJob">job</see>.
    /// </returns>
    public static IScheduledJob AddAsync(this IScheduler scheduler, ExecuteDelegate d, ISchedule schedule,
        [CallerArgumentExpression("d")] string name = "")
        => scheduler.Add(SimpleJob.CreateAsync(d, name), schedule);
}
