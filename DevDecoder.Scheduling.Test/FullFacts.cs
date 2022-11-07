// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using DevDecoder.Scheduling.Schedules;
using NodaTime;
using Xunit.Abstractions;

namespace DevDecoder.Scheduling.Test;

public class FullFacts : TestBase
{
    public FullFacts(ITestOutputHelper output) : base(output) { }

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

        var scheduler = GetScheduler();
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

        var scheduler = GetScheduler();
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

        var scheduler = GetScheduler();
        var job = scheduler.Add(FailJob,
            new LimitSchedule(2, new GapSchedule(Duration.FromMilliseconds(5), ScheduleOptions.IgnoreErrors)));
        Assert.NotNull(job);
        Assert.True(job.IsEnabled);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(2, counter);
        Assert.True(job.IsEnabled, "Job should not be disabled on error.");
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
        var scheduler = GetScheduler(TestClock.From(now));
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
        var scheduler = GetScheduler(new TestClock(_ => now));

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
        var scheduler = GetScheduler(new TestClock(_ => now));

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
}
