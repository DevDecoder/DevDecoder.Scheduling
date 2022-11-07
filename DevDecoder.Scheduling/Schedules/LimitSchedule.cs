// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Implements a <see cref="ISchedule">schedule</see> where the number of executions is limited.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="LimitSchedule" /> caches calls to the underlying <see cref="Schedule" />, so repeated calls to
///         <see cref="Next(IScheduler, ZonedDateTime)" /> with the same input date and time, will be given the same
///         result, without
///         decreasing the <see cref="Remaining" /> count.
///     </para>
///     <para>
///         However, the <see cref="Remaining" /> count does decrease every time it queries the underlying
///         <see cref="Schedule" /> and
///         get a different answer to the previous query.
///     </para>
/// </remarks>
public class LimitSchedule : ISchedule
{
    private (ZonedDateTime?, ZonedDateTime?) _last;
    private int _remaining;

    /// <summary>
    ///     Creates a new <see cref="LimitSchedule" />.
    /// </summary>
    /// <param name="remaining">The number of executions remaining.</param>
    /// <param name="schedule">The underlying schedule.</param>
    public LimitSchedule(int remaining, ISchedule schedule)
    {
        _remaining = remaining < 0 ? 0 : remaining;
        Schedule = schedule;
        Name = $"({remaining} of) {schedule.Name}";
    }

    /// <summary>
    ///     The number of executions remaining.
    /// </summary>
    public int Remaining => _remaining;

    public ISchedule Schedule { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ScheduleOptions Options => Schedule.Options;

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last)
    {
        lock (Schedule)
        {
            if (_remaining <= 0)
            {
                return null;
            }

            if (last == _last.Item1)
            {
                return _last.Item2;
            }

            var next = Schedule.Next(scheduler, last);
            if (next != _last.Item2)
            {
                // We got a different execution time.
                Interlocked.Decrement(ref _remaining);
            }

            _last = (last, next);
            return next;
        }
    }
}
