// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

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
        var counter = 0;

        void TestJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
        }

        var scheduler = GetScheduler();
        scheduler.Add(TestJob, new LimitSchedule(3, new GapSchedule(Duration.FromMilliseconds(5))));
        await Task.Delay(60);
        Assert.Equal(3, counter);
    }

    [Fact]
    public async Task ErrorsDisableExecution()
    {
        var counter = 0;

        void FailJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            throw new InvalidOperationException();
        }

        var scheduler = GetScheduler();
        var job = scheduler.Add(FailJob, new LimitSchedule(2, new GapSchedule(Duration.FromMilliseconds(5))));
        Assert.NotNull(job);
        Assert.True(job.IsEnabled);
        await Task.Delay(50);

        Assert.Equal(1, counter);
        Assert.False(job.IsEnabled, "Job should be disabled on error.");
    }

    [Fact]
    public async Task ErrorsIgnored()
    {
        var counter = 0;

        void FailJob(IJobState state)
        {
            Output.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}");
            throw new InvalidOperationException();
        }

        var scheduler = GetScheduler();
        var job = scheduler.Add(FailJob,
            new LimitSchedule(2, new GapSchedule(Duration.FromMilliseconds(5), ScheduleOptions.IgnoreErrors)));
        Assert.NotNull(job);
        Assert.True(job.IsEnabled);
        await Task.Delay(50);

        Assert.Equal(2, counter);
        Assert.True(job.IsEnabled, "Job should not be disabled on error.");
    }
}
