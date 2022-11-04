// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using NodaTime;

namespace DevDecoder.Scheduling;

public partial class Scheduler
{
    /// <summary>
    ///     Implement <see cref="IJobState" /> and <see cref="IScheduledJob" />, allowing for the executing of jobs.
    /// </summary>
    private class JobState : IJobState, IScheduledJob
    {
        private readonly IJob _job;
        private readonly object _lock = new();
        private readonly ISchedule _schedule;
        private readonly Scheduler _scheduler;
        private Task? _currentExecutionTask;
        private int _isEnabled = 1;

        public JobState(IJob job, Scheduler scheduler, ISchedule schedule, ILogger? logger)
        {
            _job = job;
            _scheduler = scheduler;
            _schedule = schedule;
            Logger = logger;
            CalculateNextDue();
        }

        /// <inheritdoc cref="IJobState.Id" />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc cref="IJobState.Scheduler" />
        public IScheduler Scheduler => _scheduler;

        /// <inheritdoc cref="IJobState.Schedule" />
        public ISchedule? Schedule => IsManual ? null : _schedule;

        /// <inheritdoc cref="IJobState.Due" />
        public ZonedDateTime? Due { get; private set; }

        /// <inheritdoc cref="IJobState.Logger" />
        public ILogger? Logger { get; }

        /// <inheritdoc cref="IJobState.IsManual" />
        public bool IsManual { get; private set; }

        /// <inheritdoc cref="IJobState.IsExecuting" />
        public bool IsExecuting => _currentExecutionTask is not null;

        /// <inheritdoc cref="IJobState.IsEnabled" />
        public bool IsEnabled
        {
            get => _isEnabled > 0;
            set
            {
                var v = value ? 1 : 0;
                if (Interlocked.Exchange(ref _isEnabled, v) == v)
                {
                    return;
                }

                CalculateNextDue();
            }
        }

        /// <inheritdoc cref="IScheduledJob.Schedule" />
        ISchedule IScheduledJob.Schedule => _schedule;

        /// <inheritdoc />
        Task IScheduledJob.ExecuteAsync(CancellationToken cancellationToken)
            => DoExecuteAsync(true, cancellationToken);

        /// <inheritdoc />
        public string Name => _job.Name;

        /// <summary>
        ///     Execute the job asynchronously, as part of running hte schedule.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
            => DoExecuteAsync(false, cancellationToken);

        private Task DoExecuteAsync(bool manual, CancellationToken cancellationToken = default)
        {
            if (!manual && _isEnabled < 1)
            {
                return Task.CompletedTask;
            }

            var task = new Task(() => _job.ExecuteAsync(this, cancellationToken),
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach);
            var current = Interlocked.CompareExchange(ref _currentExecutionTask, task, null);
            if (current != null)
            {
                task.Dispose();
                return current;
            }

            if (manual)
            {
                IsManual = true;
                Due = Scheduler.GetCurrentZonedDateTime();
            }

            // Add continuation to cleanup.
            task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Logger?.LogError(t.Exception, "The '{JobName}' job, threw an exception.", Name);
                    }

                    IsManual = false;
                    // Calculate next due.
                    CalculateNextDue();
                    Interlocked.CompareExchange(ref _currentExecutionTask, null, task);
                },
                // We always want the continuation to run.
                CancellationToken.None);

            task.Start();
            return task;
        }

        /// <summary>
        ///     Calculate the next due <see cref="ZonedDateTime"/>.
        /// </summary>
        private void CalculateNextDue()
        {
            var schedule = _schedule;
            var scheduleOptions = schedule.Options;
            lock (_lock)
            {
                if (!IsEnabled)
                {
                    if (Due is null)
                    {
                        return;
                    }

                    Due = null;
                }
                else
                {
                    var due = schedule.Next(Scheduler,
                        Due is not null && scheduleOptions.HasFlag(ScheduleOptions.FromDue)
                            ? Due.Value
                            : Scheduler.GetCurrentZonedDateTime());

                    if (due is not null && scheduleOptions > ScheduleOptions.FromDue)
                    {
                        // Round up due time.
                        var instant = due.Value.ToInstant();
                        if (scheduleOptions.HasFlag(ScheduleOptions.AlignDays))
                        {
                            instant = Instant.FromUnixTimeTicks(
                                (instant.ToUnixTimeTicks() + NodaConstants.TicksPerDay - 1) / NodaConstants.TicksPerDay *
                                NodaConstants.TicksPerDay);
                        }
                        else if (scheduleOptions.HasFlag(ScheduleOptions.AlignHours))
                        {
                            instant = Instant.FromUnixTimeTicks(
                                (instant.ToUnixTimeTicks() + NodaConstants.TicksPerHour - 1) / NodaConstants.TicksPerHour *
                                NodaConstants.TicksPerHour);
                        }
                        else if (scheduleOptions.HasFlag(ScheduleOptions.AlignMinutes))
                        {
                            instant = Instant.FromUnixTimeTicks(
                                (instant.ToUnixTimeTicks() + NodaConstants.TicksPerMinute - 1) /
                                NodaConstants.TicksPerMinute *
                                NodaConstants.TicksPerMinute);
                        }
                        else if (scheduleOptions.HasFlag(ScheduleOptions.AlignSeconds))
                        {
                            instant = Instant.FromUnixTimeTicks(
                                (instant.ToUnixTimeTicks() + NodaConstants.TicksPerSecond - 1) /
                                NodaConstants.TicksPerSecond *
                                NodaConstants.TicksPerHour);
                        }

                        due = instant.InZone(due.Value.Zone);
                    }

                    if (Due == due)
                    {
                        return;
                    }

                    Due = due;
                }
            }

            // Check the schedule as our Due date has changed.
            _scheduler.CheckSchedule();
        }
    }
}
