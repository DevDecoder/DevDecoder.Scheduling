// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DevDecoder.Scheduling.Jobs;

namespace DevDecoder.Scheduling
{
    public static class SchedulerExtensions
    {
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
        public static IScheduledJob Add(this IScheduler scheduler, Action action, ISchedule schedule, [CallerArgumentExpression("action")] string name = "")
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
        public static IScheduledJob Add(this IScheduler scheduler, Action<IJobState, CancellationToken> action, ISchedule schedule,
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
        public static IScheduledJob Add<TResult>(this IScheduler scheduler, Func<IJobState, TResult> func, ISchedule schedule,
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
        public static IScheduledJob Add<TResult>(this IScheduler scheduler, Func<IJobState, CancellationToken, TResult> func, ISchedule schedule,
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
        public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler, Func<Task<TResult>> func, ISchedule schedule,
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
        public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler, Func<IJobState, Task<TResult>> func, ISchedule schedule,
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
        public static IScheduledJob AddAsync<TResult>(this IScheduler scheduler, Func<IJobState, CancellationToken, Task<TResult>> func, ISchedule schedule,
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
}
