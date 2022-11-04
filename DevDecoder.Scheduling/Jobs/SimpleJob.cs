// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace DevDecoder.Scheduling.Jobs;

public delegate Task ExecuteDelegate(IJobState state, CancellationToken cancellationToken = default);

/// <summary>
///     A simple job can be created from any compatible action or function.
/// </summary>
public sealed class SimpleJob : IJob
{
    private const string ResultLog = "The {JobName} job returned: {Result}";
    private readonly ExecuteDelegate _delegate;

    /// <summary>
    ///     Creates an instance of <see cref="SimpleJob" />.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="delegate">The delegate.</param>
    /// <remarks>
    ///     Use the static <see cref="CreateAsync(ExecuteDelegate, string)" /> method, and its overloads, or the
    ///     <see cref="Create(Action, string)" /> method and its overloads.
    /// </remarks>
    private SimpleJob(string name, ExecuteDelegate @delegate)
    {
        Name = name;
        _delegate = @delegate;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task ExecuteAsync(IJobState state, CancellationToken cancellationToken = default) =>
        _delegate(state, cancellationToken);

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action" />.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    public static SimpleJob Create(Action action, [CallerArgumentExpression("action")] string name = "")
        => new(name, (_, _) =>
        {
            action();
            return Task.CompletedTask;
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action&lt;T1>" />.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    public static SimpleJob Create(Action<IJobState> action, [CallerArgumentExpression("action")] string name = "")
        => new(name, (status, _) =>
            {
                action(status);
                return Task.CompletedTask;
            }
        );

    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="Action&lt;T1, T2>" />.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    public static SimpleJob Create(Action<IJobState, CancellationToken> action,
        [CallerArgumentExpression("action")] string name = "")
        => new(name, (status, cancellationToken) =>
        {
            action(status, cancellationToken);
            return Task.CompletedTask;
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="func">The function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob Create<TResult>(Func<TResult> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, (status, _) =>
        {
            var result = func();
            status.Logger?.LogInformation(ResultLog, name, result);
            return Task.CompletedTask;
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="func">The function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob Create<TResult>(Func<IJobState, TResult> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, (status, _) =>
        {
            var result = func(status);
            status.Logger?.LogInformation(ResultLog, name, result);
            return Task.CompletedTask;
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from a <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="func">The function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob Create<TResult>(Func<IJobState, CancellationToken, TResult> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, (status, cancellationToken) =>
        {
            var result = func(status, cancellationToken);
            status.Logger?.LogInformation(ResultLog, name, result);
            return Task.CompletedTask;
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;TResult>" />.
    /// </summary>
    /// <param name="action">The async action.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    public static SimpleJob CreateAsync(Func<Task> action,
        [CallerArgumentExpression("action")] string name = "")
        => new(name, (_, _) => action());

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="action">The async action.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="action" />.</param>
    public static SimpleJob CreateAsync(Func<IJobState, Task> action,
        [CallerArgumentExpression("action")] string name = "")
        => new(name, (status, _) => action(status));

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="func">The async function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob CreateAsync<TResult>(Func<Task<TResult>> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, async (status, _) =>
        {
            var result = await func();
            status.Logger?.LogInformation(ResultLog, name, result);
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, TResult>" />.
    /// </summary>
    /// <param name="func">The async function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob CreateAsync<TResult>(Func<IJobState, Task<TResult>> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, async (status, _) =>
        {
            var result = await func(status);
            status.Logger?.LogInformation(ResultLog, name, result);
        });

    /// <summary>
    ///     Create a <see cref="IJob" /> from an async <see cref="Func&lt;T1, T2, TResult>" />.
    /// </summary>
    /// <param name="func">The async function.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="func" />.</param>
    /// <remarks>Note, the result of the function is logged, but otherwise ignored.</remarks>
    public static SimpleJob CreateAsync<TResult>(Func<IJobState, CancellationToken, Task<TResult>> func,
        [CallerArgumentExpression("func")] string name = "")
        => new(name, async (status, cancellationToken) =>
        {
            var result = await func(status, cancellationToken);
            status.Logger?.LogInformation(ResultLog, name, result);
        });


    /// <summary>
    ///     Create a <see cref="IJob" /> from an <see cref="ExecuteDelegate" />.
    /// </summary>
    /// <param name="d">The delegate.</param>
    /// <param name="name">The name job name. Note, this will default to the argument passed into <paramref name="d" />.</param>
    public static SimpleJob CreateAsync(ExecuteDelegate d,
        [CallerArgumentExpression("d")] string name = "")
        => new(name, d);
}
