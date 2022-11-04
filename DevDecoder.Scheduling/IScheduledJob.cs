// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     Interface that allows you to execute an existing scheduled job, manually.
/// </summary>
public interface IScheduledJob
{
    /// <summary>
    ///     A unique identified for the scheduled job.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    ///     An optional job name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The <see cref="IScheduler">scheduler</see> executing this job.
    /// </summary>
    IScheduler Scheduler { get; }

    /// <summary>
    ///     The <see cref="ISchedule">schedule</see> that triggered this execution.
    /// </summary>
    ISchedule Schedule { get; }

    /// <summary>
    ///     The <see cref="Instant">instant</see> the job was due to run.
    /// </summary>
    /// <remarks>This will be when the job was requested, if the job is executed manually; otherwise <c>null</c>.</remarks>
    public ZonedDateTime? Due { get; }

    /// <summary>
    ///     If <c>true</c> then the job is currently executing; otherwise <c>false</c>.
    /// </summary>
    bool IsExecuting { get; }

    /// <summary>
    ///     If <c>true</c> then the job is allowed to execute; otherwise <c>false</c>, prevents further executions.
    /// </summary>
    /// <remarks>This does not prevent <see cref="ExecuteAsync">manual execution</see>.</remarks>
    bool IsEnabled { get; set; }

    /// <summary>
    ///     Executes the current job manually.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
