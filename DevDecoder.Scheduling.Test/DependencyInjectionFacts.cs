// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace DevDecoder.Scheduling.Test;

public class DependencyInjectionFacts : TestBase
{
    public DependencyInjectionFacts(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AcceptsClock()
    {
        var services = GetServices();
        var expectedClock = TestClock.Never;
        services.AddSingleton<IPreciseClock>(expectedClock);
        services.AddSingleton<IScheduler, Scheduler>();

        using var serviceProvider = services.BuildServiceProvider();

        // Retrieve scheduler
        var scheduler = serviceProvider.GetService<IScheduler>();
        Assert.NotNull(scheduler);

        // Retrieve clock directly
        var actualClock = serviceProvider.GetService<IPreciseClock>();
        Assert.Equal(expectedClock, actualClock);

        // Confirm clock assigned
        Assert.Equal(expectedClock, scheduler!.Clock);
    }
}
