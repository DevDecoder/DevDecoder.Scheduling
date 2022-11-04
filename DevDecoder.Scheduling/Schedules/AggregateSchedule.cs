// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Collections;
using NodaTime;

namespace DevDecoder.Scheduling.Schedules;

/// <summary>
///     Creates a schedule made up of multiple other schedules
/// </summary>
public class AggregateSchedule : ISchedule, IEnumerable<ISchedule>
{
    private readonly ISchedule[] _schedules;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateSchedule" /> class.
    /// </summary>
    /// <param name="schedules">An enumeration of schedules.</param>
    public AggregateSchedule(IEnumerable<ISchedule> schedules)
        : this(string.Empty, schedules.ToArray())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateSchedule" /> class.
    /// </summary>
    /// <param name="name">An optional name for the schedule.</param>
    /// <param name="schedules">An enumeration of schedules.</param>
    public AggregateSchedule(string name, IEnumerable<ISchedule> schedules)
        : this(name, schedules.ToArray())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateSchedule" /> class.
    /// </summary>
    /// <param name="schedules">A collection of schedules.</param>
    public AggregateSchedule(params ISchedule[] schedules)
        : this(string.Empty, schedules)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="AggregateSchedule" /> class.
    /// </summary>
    /// <param name="name">An optional name for the schedule.</param>
    /// <param name="schedules">A collection of schedules.</param>
    /// <exception cref="System.ArgumentException">The specified schedules have differing options.</exception>
    public AggregateSchedule(string name, params ISchedule[] schedules)
    {
        if (schedules == null)
        {
            throw new ArgumentNullException(nameof(schedules));
        }

        // Duplicate collection
        _schedules = schedules.Where(s => s != null).ToArray();
        Name = name;
        if (!_schedules.Any())
        {
            Options = ScheduleOptions.None;
            return;
        }

        // Calculate options and ensure all are identical.
        var first = true;
        foreach (var schedule in _schedules.OrderBy(s => s.Name))
        {
            if (first)
            {
                Options = schedule.Options;
                first = false;
                continue;
            }

            if (schedule.Options != Options)
            {
                throw new ArgumentException("Aggregated schedules must have common options.", nameof(schedules));
            }
        }
    }

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the
    ///     collection.
    /// </returns>
    public IEnumerator<ISchedule> GetEnumerator() => ((IEnumerable<ISchedule>)_schedules).GetEnumerator();

    /// <summary>
    ///     Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last)
    {
        ZonedDateTime? next = null;
        foreach (var schedule in _schedules)
        {
            var scheduleNext = schedule.Next(scheduler, last);
            if (scheduleNext is null) continue;

            // If we were scheduled in the past return immediately.
            if (ZonedDateTime.Comparer.Instant.Compare(scheduleNext.Value, last) <= 0)
            {
                return last;
            }
            if (next is null || ZonedDateTime.Comparer.Instant.Compare(scheduleNext.Value, next.Value) < 0)
            {
                next = scheduleNext;
            }
        }

        return next;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ScheduleOptions Options { get; }

    /// <inheritdoc />
    public override string ToString() => $"Aggregate Schedule ({_schedules.Length} schedules)";
}
