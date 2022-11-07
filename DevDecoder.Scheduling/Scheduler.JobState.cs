// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
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
        private ZonedDateTime? _due;
        private int _isEnabled = 1;

        public JobState(IJob job, Scheduler scheduler, ISchedule schedule, ILogger? logger)
        {
            _job = job;
            _scheduler = scheduler;
            _schedule = schedule;
            Logger = logger;
        }

        /// <inheritdoc cref="IJobState.Id" />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc cref="IJobState.Scheduler" />
        public IScheduler Scheduler => _scheduler;

        /// <inheritdoc cref="IJobState.Schedule" />
        public ISchedule? Schedule => IsManual ? null : _schedule;

        /// <inheritdoc cref="IJobState.Due" />
        ZonedDateTime IJobState.Due => _due!.Value;

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

                CalculateNextDue(true);
                _scheduler.CheckSchedule($"Job '{Name}' being enabled.");
            }
        }

        /// <inheritdoc cref="IJobState.Name" />
        public string Name => _job.Name;

        /// <inheritdoc cref="IScheduledJob.Due" />
        public ZonedDateTime? Due => _scheduler.IsEnabled ? _due : null;

        /// <inheritdoc cref="IScheduledJob.Schedule" />
        ISchedule IScheduledJob.Schedule => _schedule;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task IScheduledJob.ExecuteAsync(CancellationToken cancellationToken)
            => DoExecuteAsync(true, cancellationToken);

        /// <summary>
        ///     Execute the job asynchronously, as part of running the schedule.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            // If executing manually, set the due time to now, and set IsManual
            if (manual)
            {
                IsManual = true;
                _due = Scheduler.GetCurrentZonedDateTime();
            }

            // Add continuation to cleanup.
            task.ContinueWith(t =>
                {
                    IsManual = false;
                    if (t.IsFaulted)
                    {
                        var l = Logger;
                        if (l is not null && l.IsEnabled(LogLevel.Error))
                        {
                            var exceptions = t.Exception?.InnerExceptions;
                            if (exceptions is not null)
                            {
                                foreach (var exception in exceptions)
                                {
                                    Logger?.LogError(exception, "The '{JobName}' job, threw an exception.", Name);
                                }
                            }
                            else
                            {
                                Logger?.LogError("The '{JobName}' job, threw an exception.", Name);
                            }
                        }

                        if (!_schedule.Options.HasFlag(ScheduleOptions.IgnoreErrors))
                        {
                            // Disable the job as failed.
                            IsEnabled = false;
                        }
                    }

                    // Mark job as no longer executing, need to do this before re-calculating schedules.
                    Interlocked.CompareExchange(ref _currentExecutionTask, null, task);

                    // Calculate next due.
                    CalculateNextDue();

                    Logger?.LogDebug("Finished '{JobName}' job at {Now}, next due {Due}", Name,
                        Scheduler.GetCurrentZonedDateTime(),
                        _due);

                    // Re-check schedule
                    _scheduler.CheckSchedule($"Job '{Name}' being completed.");
                },
                // We always want the continuation to run.
                CancellationToken.None);

            Logger?.LogDebug("Starting '{JobName}' job at {Now}, due {Due}", Name, Scheduler.GetCurrentZonedDateTime(),
                _due);
            task.Start();
            return task;
        }

        /// <summary>
        ///     Calculate the next due <see cref="ZonedDateTime" />.
        /// </summary>
        public void CalculateNextDue(bool force = false)
        {
            var schedule = _schedule;
            var scheduleOptions = schedule.Options;
            lock (_lock)
            {
                if (!IsEnabled)
                {
                    if (_due is null)
                    {
                        return;
                    }

                    _due = null;
                }
                else
                {
                    var due = schedule
                        .Next(Scheduler,
                            !force && _due is not null && scheduleOptions.HasFlag(ScheduleOptions.FromDue)
                                ? _due.Value
                                : Scheduler.GetCurrentZonedDateTime())
                        .ApplyOptions(scheduleOptions);

                    if (_due == due)
                    {
                        return;
                    }

                    _due = due;
                }
            }
        }
    }
}
