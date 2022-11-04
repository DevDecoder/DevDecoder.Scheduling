// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using Xunit.Abstractions;

namespace DevDecoder.Scheduling.Test;

public abstract class TestBase
{
    protected TestBase(ITestOutputHelper output) => Output = output;

    protected ITestOutputHelper Output { get; }

    protected IServiceCollection GetServices() =>
        new ServiceCollection()
            .AddLogging(builder => builder.AddXUnit(Output).SetMinimumLevel(LogLevel.Trace));

    protected IScheduler GetScheduler(
        IPreciseClock? clock = null,
        Duration? maximumExecutionDuration = null,
        IDateTimeZoneProvider? dateTimeZoneProvider = null)
    {
        var services = GetServices();
        services.AddSingleton(clock ?? StandardClock.Instance);
        services.AddSingleton(dateTimeZoneProvider ?? DateTimeZoneProviders.Bcl);
        services.AddSingleton<IScheduler>(sp => new Scheduler(
            sp.GetRequiredService<IPreciseClock>(),
            maximumExecutionDuration,
            sp.GetRequiredService<IDateTimeZoneProvider>(),
            sp.GetRequiredService<ILogger<Scheduler>>()));
        return services.BuildServiceProvider().GetRequiredService<IScheduler>();
    }
}
