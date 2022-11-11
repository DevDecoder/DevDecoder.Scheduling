// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using DevDecoder.Scheduling.Schedules;
using NodaTime;
using Xunit.Abstractions;

namespace DevDecoder.Scheduling.Test;

public class SchedulerFacts : TestBase
{
    public SchedulerFacts(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task LimitScheduleAccuratelyLimitsExecutionCount()
    {
        var tcs = new TaskCompletionSource();
        var counter = 0;

        void TestJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            if (counter >= 3)
            {
                tcs.TrySetResult();
            }
        }

        using var scheduler = GetScheduler();
        scheduler.Add(TestJob, new LimitSchedule(3, new GapSchedule(Duration.FromMilliseconds(5))));
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal(3, counter);
    }

    [Fact]
    public async Task ErrorsDisableExecution()
    {
        var tcs = new TaskCompletionSource();
        var counter = 0;

        void FailJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            tcs.TrySetResult();
            throw new InvalidOperationException();
        }

        using var scheduler = GetScheduler();
        var job = scheduler.Add(FailJob, new LimitSchedule(2, new GapSchedule(Duration.FromMilliseconds(5))));
        Assert.NotNull(job);
        Assert.True(job.IsEnabled);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        // Leave some more time, in case the schedule is still running (it shouldn't be).
        await Task.Delay(50);

        Assert.Equal(1, counter);
        Assert.False(job.IsEnabled, "Job should be disabled on error.");
    }

    [Fact]
    public async Task ErrorsIgnored()
    {
        var tcs = new TaskCompletionSource();
        var counter = 0;

        void FailJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            if (counter >= 2)
            {
                tcs.TrySetResult();
            }

            throw new InvalidOperationException();
        }

        using var scheduler = GetScheduler();
        var job = scheduler.Add(FailJob,
            new LimitSchedule(2, new GapSchedule(Duration.FromMilliseconds(5), ScheduleOptions.IgnoreErrors)));
        Assert.NotNull(job);
        Assert.True(job.IsEnabled);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(2, counter);
        Assert.True(job.IsEnabled, "Job should not be disabled on error.");
    }

    [Fact]
    public async Task JobStateValid()
    {
        var tcs = new TaskCompletionSource();

        void TestJob(IJobState state)
        {
            Assert.True(state.IsEnabled);
            Assert.True(state.IsExecuting);
            Assert.False(state.IsManual);
            tcs.TrySetResult();
        }

        // Use test-clock that will advance by one second each call.
        using var scheduler = GetScheduler();

        var job = scheduler.Add(TestJob, new OneOffSchedule(scheduler.GetCurrentZonedDateTime().PlusMilliseconds(10)));

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(job.IsEnabled);

        // Yield to allow job continuation to set 'IsExecuting' back to false.
        await Task.Delay(10);
        Assert.False(job.IsExecuting);
    }

    [Fact]
    public async Task JobStateNotesManualExecution()
    {
        var counter = 0;

        async Task TestJob(IJobState state, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref counter);
            Assert.True(state.IsEnabled);
            Assert.True(state.IsExecuting);
            Assert.True(state.IsManual);
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            Assert.True(cancellationToken.IsCancellationRequested);
        }

        // Use test-clock that will advance by one second each call.
        using var scheduler = GetScheduler();

        var cts = new CancellationTokenSource(10);

        // Never run automatically
        var job = scheduler.AddAsync(TestJob, OneOffSchedule.Never);

        // Running job twice should de-bounce.
        await Task.WhenAll(job.ExecuteAsync(cts.Token), job.ExecuteAsync(cts.Token));

        // Should only have run once.
        Assert.Equal(1, counter);
        Assert.True(job.IsEnabled);
        Assert.False(job.IsExecuting);
    }

    [Fact]
    public async Task SecondsAreAligned()
    {
        var tcs = new TaskCompletionSource();

        // Create test times
        var now = Instant.FromUtc(2023, 1, 1, 0, 0, 0).Plus(Duration.FromMilliseconds(500));
        var nowZdt = now.InUtc();

        ZonedDateTime? due = null;

        void TestJob(IJobState state)
        {
            due = state.Due;
            tcs.TrySetResult();
        }

        // Use test-clock that will advance by one second each call.
        using var scheduler = GetScheduler(TestClock.From(now));
        scheduler.DateTimeZone = DateTimeZone.Utc;

        var job = scheduler.Add(TestJob,
            new OneOffSchedule(nowZdt.PlusMilliseconds(10), ScheduleOptions.AlignSeconds));
        Assert.Equal(nowZdt.PlusMilliseconds(500), job.Due);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.NotNull(due);
        // Check due time was aligned
        Assert.Equal(0, due.Value.TickOfSecond);
        Assert.Equal(nowZdt.PlusMilliseconds(500), due);
    }

    [Fact]
    public async Task LongScheduleSupport()
    {
        // Create test times
        var now = Instant.FromUtc(2023, 1, 1, 0, 0, 0).Plus(Duration.FromMilliseconds(500));
        var nowZdt = now.InUtc();

        void TestJob(IJobState state)
        {
        }

        // Use test-clock that will advance by one second each call.
        using var scheduler = GetScheduler(TestClock.From(now, Duration.FromDays(1)));
        scheduler.DateTimeZone = DateTimeZone.Utc;

        // Create schedule with delay of 100 days, which is much more than allowed timer delay.
        var job = scheduler.Add(TestJob,
            new OneOffSchedule(nowZdt.PlusHours(2400)));
        Assert.Equal(nowZdt.PlusHours(2400), job.Due);

        await Task.Delay(10);

        Assert.True(job.IsEnabled);
    }

    [Fact]
    public async Task DisablingJobPreventsExecution()
    {
        var counter = 0;
        var tcs = new TaskCompletionSource();

        void TestJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            tcs.TrySetResult();
        }

        // Create test times
        var now = Instant.FromUtc(2023, 1, 1, 0, 0, 0);

        // Test clock that we can control using the 'now' variable.
        // ReSharper disable once AccessToModifiedClosure
        using var scheduler = GetScheduler(new TestClock(_ => now));

        // Create job to run every second.
        var job = scheduler.Add(TestJob, new GapSchedule(Duration.FromSeconds(1)));
        Assert.Equal(now.InZone(scheduler.DateTimeZone).PlusSeconds(1), job.Due);
        job.IsEnabled = false;

        // Move time 
        now = now.Plus(Duration.FromSeconds(3));
        Assert.Equal(0, counter);
        Assert.Null(job.Due);

        job.IsEnabled = true;
        Assert.NotNull(job.Due);
        Assert.Equal(now.InZone(scheduler.DateTimeZone).PlusSeconds(1), job.Due);

        // Move on time
        now = now.Plus(Duration.FromSeconds(1));
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1.5));

        Assert.Equal(1, counter);
    }

    [Fact]
    public async Task DisablingSchedulerPreventsExecution()
    {
        var counter = 0;
        var tcs = new TaskCompletionSource();

        void TestJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            tcs.TrySetResult();
        }

        // Create test times
        var now = Instant.FromUtc(2023, 1, 1, 0, 0, 0);

        // Test clock that we can control using the 'now' variable.
        // ReSharper disable once AccessToModifiedClosure
        using var scheduler = GetScheduler(new TestClock(_ => now));

        // Create job to run every second.
        var job = scheduler.Add(TestJob, new GapSchedule(Duration.FromSeconds(1)));
        Assert.Equal(now.InZone(scheduler.DateTimeZone).PlusSeconds(1), job.Due);
        scheduler.IsEnabled = false;

        // Move time 
        now = now.Plus(Duration.FromSeconds(3));
        Assert.Equal(0, counter);
        Assert.Null(job.Due);

        scheduler.IsEnabled = true;
        Assert.NotNull(job.Due);
        Assert.Equal(now.InZone(scheduler.DateTimeZone).PlusSeconds(1), job.Due);

        // Move on time
        now = now.Plus(Duration.FromSeconds(1));
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1.5));

        Assert.Equal(1, counter);
    }

    [Fact]
    public void TryRemoveDisablesAndDetachesScheduler()
    {
        using var scheduler = GetScheduler();
        var job = scheduler.Add(() => { } /* Do Nothing */, new GapSchedule(Duration.FromMilliseconds(100)));
        Assert.True(job.IsEnabled);
        Assert.Equal(scheduler, job.Scheduler);
        Assert.NotNull(job.Due);

        Assert.True(scheduler.TryRemove(job));
        Assert.False(job.IsEnabled);
        Assert.Null(job.Scheduler);
        Assert.Null(job.Due);

        // Even setting to enabled doesn't 'stick'.
        job.IsEnabled = true;
        Assert.False(job.IsEnabled);
    }

    [Fact]
    public async Task DoubleDisposeOk()
    {
        using var scheduler = GetScheduler();

        // Create rapid action to keep spinning.
        scheduler.Add(() => { } /* Do Nothing */, new GapSchedule(Duration.FromMilliseconds(1)));

        await Task.Delay(TimeSpan.FromSeconds(0.1));

        // Let's dispose twice.
        scheduler.Dispose();
    }
}
