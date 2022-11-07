// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using DevDecoder.Scheduling.Clocks;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace DevDecoder.Scheduling;

public partial class Scheduler : IScheduler, IDisposable
{
    /// <summary>
    ///     The minimum time the timer will wait for, otherwise uses a <see cref="SpinWait" />.
    /// </summary>
    private static readonly Duration s_minimumTimerWait = Duration.FromMilliseconds(1);

    /// <summary>
    ///     The maximum time span supported by a timer (in milliseconds) ~49 days!
    /// </summary>
    private static readonly Duration s_maximumTimerWait = Duration.FromMilliseconds(0xfffffffe);

    /// <summary>
    ///     The logger.
    /// </summary>
    private readonly ILogger<Scheduler>? _logger;

    /// <summary>
    ///     Holds jobs based on their schedule.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, JobState> _scheduledJobs = new();

    /// <summary>
    ///     The check execution counter.
    /// </summary>
    private long _checkCount;

    /// <summary>
    ///     Whether we are enabled, initialize as enabled.
    /// </summary>
    private int _enabled = 1;

    /// <summary>
    ///     The master cancellation token source allows us to cancel all ongoing jobs on disposal.
    /// </summary>
    private CancellationTokenSource? _masterCancellationTokenSource = new();

    /// <summary>
    ///     The background timer.
    /// </summary>
    private Timer? _ticker;

    /// <summary>
    ///     The tick state.
    ///     &lt;0 = Disposed
    ///     0     = Inactive
    ///     1     = Running
    ///     >1    = Running, and more checks needed.
    /// </summary>
    private int _tickState;


    /// <summary>
    ///     Creates a scheduler using the <see cref="ClockPrecision">specified clock precision</see>.
    /// </summary>
    /// <remarks>
    ///     Note: if the precision is not available on this machine, the
    ///     <see cref="StandardClock.Instance">standard clock</see> is used.
    /// </remarks>
    /// <param name="precision">The requested precision.</param>
    /// <param name="maximumExecutionDuration">The maximum execution duration.</param>
    /// <param name="dateTimeZoneProvider">The <see cref="IDateTimeZoneProvider">date/time zone provider</see>.</param>
    /// <param name="logger">The optional logger.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Scheduler(ClockPrecision precision, Duration? maximumExecutionDuration = null,
        IDateTimeZoneProvider? dateTimeZoneProvider = null,
        ILogger<Scheduler>? logger = null) : this(
        precision.GetClock(),
        maximumExecutionDuration,
        dateTimeZoneProvider,
        logger)
    {
    }

    /// <summary>
    ///     Creates a new scheduler.
    /// </summary>
    /// <param name="maximumExecutionDuration">The maximum execution duration.</param>
    /// <param name="clock"></param>
    /// <param name="dateTimeZoneProvider">The <see cref="IDateTimeZoneProvider">date/time zone provider</see>.</param>
    /// <param name="logger">The optional logger.</param>
    public Scheduler(IPreciseClock? clock = null, Duration? maximumExecutionDuration = null,
        IDateTimeZoneProvider? dateTimeZoneProvider = null, ILogger<Scheduler>? logger = null)
    {
        Clock = clock ?? StandardClock.Instance;
        DateTimeZoneProvider = dateTimeZoneProvider ?? DateTimeZoneProviders.Bcl;
        DateTimeZone = DateTimeZoneProvider.GetSystemDefault();
        _logger = logger;
        _ticker = new Timer(CheckSchedule, "By timer tick.", Timeout.Infinite, Timeout.Infinite);
        MaximumExecutionDuration = maximumExecutionDuration ?? Duration.MaxValue;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="Scheduler" /> is enabled.
    /// </summary>
    /// <value><see langword="true" /> if enabled; otherwise, <see langword="false" />.</value>
    public bool IsEnabled
    {
        get => _enabled > 0;
        set
        {
            if (!value)
            {
                Interlocked.CompareExchange(ref _enabled, 0, 1);
                return;
            }

            if (Interlocked.CompareExchange(ref _enabled, 1, 0) > 0)
            {
                return;
            }

            // We have been enabled, recheck schedule.
            CheckSchedule("On scheduler being enabled.");
        }
    }

    /// <summary>
    ///     Gets a value indicating when the next job is due to execute.
    /// </summary>
    public Instant NextDue { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        Interlocked.Exchange(ref _tickState, int.MinValue);
        Interlocked.Exchange(ref _ticker, null)?.Dispose();
        if (Interlocked.Exchange(ref _masterCancellationTokenSource, null) is not { } cts)
        {
            return;
        }

        // Cancel ongoing jobs.
        cts.Cancel();
        cts.Dispose();
    }

    /// <inheritdoc />
    public IPreciseClock Clock { get; }

    /// <inheritdoc />
    public IDateTimeZoneProvider DateTimeZoneProvider { get; }

    /// <inheritdoc />
    public DateTimeZone DateTimeZone { get; set; }

    /// <inheritdoc />
    public Duration MaximumExecutionDuration { get; set; }

    /// <inheritdoc />
    public ZonedDateTime GetCurrentZonedDateTime() => Clock.GetCurrentInstant().InZone(DateTimeZone);

    /// <inheritdoc />
    public IScheduledJob Add(IJob job, ISchedule schedule)
    {
        var jobState = new JobState(job, this, schedule, _logger);
        _scheduledJobs.TryAdd(jobState.Id, jobState);

        // We have to calculate the first due date, but we need to have added it to the scheduled jobs before we do.
        jobState.CalculateNextDue();

        // Check the schedule as our Due date has changed.
        CheckSchedule("Job added.");
        return jobState;
    }

    /// <inheritdoc />
    public bool TryRemove(IScheduledJob job)
        => _scheduledJobs.TryRemove(job.Id, out _);

    /// <summary>
    ///     The background thread that checks the schedule
    /// </summary>
    /// <param name="state">The reason for the check.</param>
    private void CheckSchedule(object? state = null)
    {
        var reason = $"#{Interlocked.Increment(ref _checkCount)} {state}";

        _logger?.LogTrace("Scheduler check requested: {reason}", reason);
        // Ensure ticker is stopped.
        _ticker?.Change(Timeout.Infinite, Timeout.Infinite);

        // Only allow one check to run at a time, namely the check that caused the tick state to move from 0 to 1.
        // The increment will force any currently executing check to recheck.
        var tickerState = Interlocked.Increment(ref _tickState);
        if (tickerState != 1)
        {
            return;
        }

        do
        {
            Duration wait;
            do
            {
                // Check if we're disabled.
                if (_enabled < 1)
                {
                    _logger?.LogTrace("Scheduler check terminated as no longer enabled: {reason}", reason);
                    Interlocked.Exchange(ref _tickState, 0);
                    return;
                }

                // Set our tick state to 1, we're about to do a complete check of the actions if any other tick calls
                // come in from this point onwards we will need to recheck.
                tickerState = Interlocked.Exchange(ref _tickState, 1);
                if (tickerState < 0)
                {
                    _logger?.LogTrace("Scheduler check terminated as disposed: {reason}", reason);
                    // We're disposed
                    return;
                }

                var nextInstant = Instant.MaxValue;
                foreach (var job in _scheduledJobs.Values.Where(j => j.IsEnabled && !j.IsExecuting))
                {
                    if (_tickState < 0)
                    {
                        _logger?.LogTrace("Scheduler check terminated as disposed: {reason}", reason);
                        // Disposed
                        return;
                    }

                    var due = job.Due;
                    if (due is null)
                    {
                        continue;
                    }

                    var instant = due.Value.ToInstant();
                    if (instant <= Clock.GetCurrentInstant())
                    {
                        var maximumExecutionDuration = MaximumExecutionDuration;
                        var token = _masterCancellationTokenSource?.Token ?? default;
                        if (token == default)
                        {
                            _logger?.LogTrace("Scheduler check terminated as disposed: {reason}", reason);
                            // Must have been disposed.
                            return;
                        }

                        if (maximumExecutionDuration == Duration.MaxValue ||
                            ((IScheduledJob)job).Schedule.Options.HasFlag(ScheduleOptions.LongRunning))
                        {
                            job.ExecuteAsync(token);
                        }
                        else
                        {
                            var cts = new CancellationTokenSource(maximumExecutionDuration.ToTimeSpan());
                            var jts = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);
                            job.ExecuteAsync(jts.Token).ContinueWith(_ => cts.Dispose(), CancellationToken.None);
                        }
                    }
                    else if (instant < nextInstant)
                    {
                        nextInstant = instant;
                    }
                }

                // If the tick state has increased, check again.
                if (_tickState > 1)
                {
                    _logger?.LogTrace("Scheduler check repeating due to more requests being received: {reason}",
                        reason);
                    // Yield to allow the current actions some chance to run.
                    Thread.Yield();
                    continue;
                }

                NextDue = nextInstant;
                // If the next due time is max value, we're never due
                if (nextInstant == Instant.MaxValue)
                {
                    // Set to infinite wait.
                    wait = Duration.MaxValue;
                    // Update properties.
                    break;
                }

                var now = Clock.GetCurrentInstant();

                // If we're due in the future calculate how long to wait.
                if (nextInstant > now)
                {
                    wait = nextInstant - now;
                    if (wait > Duration.Zero)
                    {
                        // Check we're waiting at least a millisecond.
                        if (wait > s_minimumTimerWait)
                        {
                            // Ensure the wait duration doesn't exceed the maximum supported by Timer.
                            if (wait > s_maximumTimerWait)
                            {
                                wait = s_maximumTimerWait;
                            }

                            break;
                        }

                        // Use a spin wait instead.
                        var spinWait = new SpinWait();
                        while (wait > Duration.Zero)
                        {
                            spinWait.SpinOnce();
                            wait = nextInstant - Clock.GetCurrentInstant();
                        }
                    }
                }

                // We're due in the past so try again.
                _logger?.LogTrace("Scheduler check repeating due to pending job: {reason}", reason);
                Thread.Yield();
            } while (true);

            // Set the ticker to run after the wait period.
            if (wait < Duration.MaxValue)
            {
                var ms = (int)wait.TotalMilliseconds;
                _logger?.LogTrace("Scheduler waiting for {wait}ms.", ms);
                _ticker?.Change(ms, Timeout.Infinite);
            }
            else
            {
                _ticker?.Change(Timeout.Infinite, Timeout.Infinite);
            }

            // Try to set the tick state back to 0, from 1 and finish
            tickerState = Interlocked.CompareExchange(ref _tickState, 0, 1);
            if (tickerState < 0)
            {
                _logger?.LogTrace("Scheduler check terminated as disposed: {reason}", reason);
                return;
            }

            if (tickerState < 2)
            {
                _logger?.LogTrace("Scheduler check ending as no more pending jobs: {reason}", reason);
                return;
            }

            // The tick state managed to increase from 1 before we could exit, so we need to clear the ticker and recheck.
            _ticker?.Change(Timeout.Infinite, Timeout.Infinite);

            _logger?.LogTrace("Scheduler check repeating due to more requests being received: {reason}", reason);
        } while (true);
    }
}
