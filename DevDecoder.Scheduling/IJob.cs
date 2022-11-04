// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

namespace DevDecoder.Scheduling;

/// <summary>
///     Interface defining a job that can run on the current <see cref="IScheduler">scheduler</see>.
/// </summary>
public interface IJob
{
    /// <summary>
    ///     An optional job name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Executes the current job.
    /// </summary>
    /// <param name="state">Information regarding the current job, (see <see cref="IJobState" />).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(IJobState state, CancellationToken cancellationToken = default);
}
