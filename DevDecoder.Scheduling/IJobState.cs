// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     Holds information for the currently executing <see cref="IJob">job</see>.
/// </summary>
public interface IJobState
{
    /// <summary>
    ///     A unique identified for the scheduled job.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     The <see cref="IScheduler">scheduler</see> executing this job.
    /// </summary>
    public IScheduler Scheduler { get; }

    /// <summary>
    ///     The <see cref="ISchedule">schedule</see> that triggered this execution.
    /// </summary>
    /// <remarks>Will be <c>null</c> if the job was <see cref="IsManual">executed manually</see>.</remarks>
    public ISchedule? Schedule { get; }

    /// <summary>
    ///     The <see cref="Instant">instant</see> the job was due to run.
    /// </summary>
    /// <remarks>This will be when the job was requested, if the job is executed manually; otherwise <c>null</c>.</remarks>
    public ZonedDateTime? Due { get; }

    /// <summary>
    ///     The current logger, if any; otherwise <c>null</c>.
    /// </summary>
    ILogger? Logger { get; }

    /// <summary>
    ///     If <c>true</c> then the current execution was triggered manually; otherwise <c>false</c>.
    /// </summary>
    bool IsManual { get; }

    /// <summary>
    ///     If <c>true</c> then the job is currently executing; otherwise <c>false</c>.
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    ///     If <c>true</c> then the job is allowed to execute; otherwise <c>false</c>, prevents further executions.
    /// </summary>
    bool IsEnabled { get; set; }
}
