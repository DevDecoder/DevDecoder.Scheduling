[![Publish](https://github.com/DevDecoder/DevDecoder.Scheduling/workflows/Build%20and%20Publish/badge.svg)](https://github.com/DevDecoder/DevDecoder.Scheduling/actions?query=workflow%3A%22Build+and+Publish%22)
[![Nuget](https://img.shields.io/nuget/v/DevDecoder.Scheduling)](https://www.nuget.org/packages/DevDecoder.Scheduling/)

# Description
This library provides a cross-platform service easily creating and running tasks on a schedule. It makes extensive use of [NodaTime](https://nodatime.org/) to allow for robust time zone handling, as well as [Cronos](https://github.com/HangfireIO/Cronos) for Cron expression support. Importantly, it is well architected to support testing, dependency injection and modern design methodologies.

# Installation
The library is [available via NuGet](https://www.nuget.org/packages?q=DevDecoder.Scheduling) and is delivered via NuGet Package Manager:

```
Install-Package DevDecoder.Scheduling
```

If you are targeting .NET Core, use the following command:

```
dotnet add package 
Install-Package DevDecoder.Scheduling
```

# Usage
The package exposes several key interfaces, and implementations that allow for easily creating and running tasks on a schedule.

For example:
```csharp
// Only create one scheduler per application, and dispose when finished with it.
using var scheduler = new Scheduler();
...

// Run a job 3 times with a gap of 5 seconds between each execution.
var job = scheduler.Add(state => Console.WriteLine($"Execution {++counter}, due: {state.Due:ss.fffffff}"),
    new LimitSchedule(3, new GapSchedule(Duration.FromSeconds(5))));

// We can use the returned job, to execute manually or disable the job temporarily, etc..
```

## IScheduler

### Initialisation
The `DevDecoder.Scheduling.IScheduler` interface is the main scheduler service, it is implemented by `DevDecoder.Scheduling.Scheduler` concrete type.  Note it also implements `IDisposable`, so should be disposed when your application terminates.

You can create a new scheduler in the main entry point of your code:
```csharp
using var scheduler = new Scheduler();
```

However, the library is designed to be used with a dependency injection framework. You can register the service as a singleton, and have it injected automatically into your other services, or retrieve it manually:
```csharp
// Add as a singleton, accessible by its interface.
services.AddSingleon<IScheduler, Scheduler>();
...
// Retrieve manually from the service provider
var scheduler = serviceProvider.GetService<IScheduler>();
```

Modern DI frameworks should correctly handle instantiation and disposal automatically, as well as suppplying a logger if registered.

### Specifying a Clock
The scheduler retrieves the current time from an `IPreciseClock`.  There are 4 clocks provided, which should cover every eventuality but you can easily create your own if desired.

* `StandardClock` - This is equivalent to using the built in `DateTime.UTCNow` function, which is usually accurate to ~100ns.  It is the default choice, an suitable for most applications.
* `FastClock` - This uses the [Query Performance Counters](https://learn.microsoft.com/en-us/windows/win32/api/profileapi/nf-profileapi-queryperformancecounter) to get the most accurate timestamp, however, it is not synchronized to any external source, though it is often accurate to <100 clock cycles.  On some systems, the clock is not available (see `FastClock.IsAvailable`, and so defaults to the `StandardClock`).  It is rare that this clock is necessary.
* `SynchronizedClock` - This clock uses [GetSystemTimePreciseAsFileTime](https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtimepreciseasfiletime) to get an accurate, synchronized time, where available. This is recommended for scenarios where it is important for multiple machines to stay in synch, e.g. during networking scenarios.
* `TestClock` - This clock can be used during testing to allow you to control what times are returned when querying. It accepts a function that, when given an `Instant` returns the next `Instant`.  Two static functions `Fixed`, which supplies a clock that returns the same `Instant` every time, and `From` which provides a clock that returns an `Instant` from a specific time, and increments by a set `Duration` each time it is queried, are supplied for convenience. There is also `TestClock.Never` which always returns `Instant.MaxValue` (the 'end of time'!).

All clocks have a static `Instance` property that can get their singleton implementation, and this can be supplied directly to the `Scheduler` on creation:
```csharp
// Use the Synchronized clock.
using var scheduler = new Scheduler(SynchronizedClock.Instance);
```

However, you can also specify the `ClockPrecision` enumeration, e.g.
```csharp
// Use the Synchronized clock.
using var scheduler = new Scheduler(ClockPrecision.Synchronized);
```

Using dependency injection:
```csharp
// Add the clock singleton to the services collection.
services.AddSingleton<IPreciseClock>(SynchronizedClock.Instance);
// Add as a singleton, accessible by its interface.
services.AddSingleon<IScheduler, Scheduler>();
```

### Timezone handling
The schedule uses `NodaTime` to ensure it handles timezones accurately.  To that end it can be injected with an `IDateTimeZoneProvider` on creation. 

### Logging
The `Scheduler` constructor accepts an [`ILogger<Scheduler>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1?view=dotnet-plat-ext-3.1&viewFallbackFrom=netstandard-2.1) for logging, this is normally injected via dependency injection, but here is an example of a [simple console logger](https://github.com/DevDecoder/HIDDevices/blob/master/HIDDevices.Sample/SimpleConsoleLogger.cs).

### Maximum Execution duration
The `MaximumExecutionDuration` can also be specified during creation, or via the `IScheduler` interface.  This defaults to `Duration.MaxValue`, but it is recommended you set this to a lower value before executing any jobs on the scheduler.  It will set any job, that doesn't have the `ScheduleOptions.LongRunning` flag set, to cancel after the duration has elapsed.

## ISchedule
The scheduler runs jobs on a schedule.  These can be as complex as your imagination allows, so long as they implement `ISchedule`, in particular:
```csharp
ZonedDateTime? Next(IScheduler scheduler, ZonedDateTime last);
```

That is, given the last time a job started, or completed (based on whether the `ScheduleOptions.FromDue` flag is set for the schedule), it needs to return the next time the job should execute.  On first execution, or first running after being disabled, this will be the current date and time.

The following schedules are built in for convenience:

### OneOffSchedule
The one off schedule allows a task to run once, at a fixed date and time, e.g.
```csharp
// Execute at midday (UTC) on 1st January 2023
new OneOffSchedule(new ZonedDateTime(Instant.FromUtc(2023, 1, 1, 12, 0), DateTimeZone.Utc));
```

### GapSchedule
The gap schedule allows a task to run repeatedly, with a fixed interval.  The interval can be measured from the start of the proceeding execution, or from it's conclusion, using the `ScheduleOptions.FromDue` flag, e.g.
```csharp
// Execute with a 5 second gap between the start of each invocation.
new GapSchedule(Duration.FromSeconds(5), ScheduleOptions.FromDue);
```

### FunctionalSchedule
The functional schedule is a convenience class that accepts a lambda to calculate the next date/time, e.g.
```csharp
// Execute every 10 seconds (rounded up to nearest second).
new FunctionalSchedule(t => t.PlusSeconds(10), ScheduleOptions.AlignSeconds | ScheduleOptions.FromDue);
```

### LimitSchedule
The limit schedule wraps any schedule, limiting how many times it will execute, e.g.:
```csharp
// Execute 3 times, with a 5ms gap between each execution.
new LimitSchedule(3, new GapSchedule(Duration.FromMilliseconds(5)))
```

### AggregateSchedule
The aggregate schedule is extremely powerful as it allows you to combine multiple schedules together, e.g.:
```csharp
// Execute every 5th second (aligned) and every 3rd second (for the first 3 times).
new AggregateSchedule(
    new GapSchedule(Duration.FromSeconds(5), ScheduleOptions.AlignSeconds),
    new LimitSchedule(3, 
        new FunctionalSchedule(t => t.PlusSeconds(3), ScheduleOptions.AlignSeconds)));
```

### CronSchedule
Finally, we also expose a schedule that can accept any [chron expression](https://github.com/HangfireIO/Cronos), e.g.:
```csharp
// Execute every 2 minutes from 1:00 AM to 01:15 AM and from 1:45 AM to 1:59 AM and at 1:30 AM
new CronSchedule("30,45-15/2 1 * * *")
// Note we also support the including the optional seconds format:
new CronSchedule("0 30,45-15/2 1 * * *", CronFormat.IncludeSeconds)
```

### ScheduleOptions
Every schedule also exposes the `ScheduleOptions` flags which have the following meanings:

| Flag | Effect when set |
| - | - |
| LongRunning | The job will not be limited by the `MaximumExecutionDuration`. |
| IgnoreErrors | The job will not be disabled when an exception is thrown. |
| FromDue | The `ISchedule.Next` method will be called with the time the previous execution was due to start, rather than when it finished. |
| AlignSeconds | The result of `ISchedule.Next` will be rounded up to the nearest second by the scheduler. |
| AlignMinutes | The result of `ISchedule.Next` will be rounded up to the nearest minute by the scheduler. |
| AlignHours | The result of `ISchedule.Next` will be rounded up to the nearest hour by the scheduler. |
| AlignDays | The result of `ISchedule.Next` will be rounded up to the nearest midnight by the scheduler. |

## Jobs
Any object that implements the simple `IJob` interface can be passed to the scheduler for scheduling:
```csharp
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
```

However, there is also a convenient `SimpleJob` class that allows the creation of any job from an action or function, using one of the `SimpleJob.Create` or `SimpleJob.CreateAsync` overloads.  Most conveniently though, there are numerouse extension methods on `IScheduler` that overload the `Add` method to create a `SimpleJob` automatically, e.g.:

```csharp
// Run a simple function in 10s, logging it's result on completion.
scheduler.Add(state => $"Log this result {state.Due}",
    new OneOffSchedule(scheduler.GetCurrentZonedDateTime().PlusSeconds(10)));
```

Although the scheduling system is really designed to run actions, if you supply `SimpleJob` with a function, it will log any result returned, e.g.
```
[2022-11-07 18:50:41Z] info: DevDecoder.Scheduling.Scheduler[0]
      The 'state => $"Log this result {state.Due}"' job returned: Log this result 2022-11-07T18:50:41 GMT Standard Time (+00)
```

You will also note that jobs are automatically named based on the arguments passed into the function/action, for easier debugging.

### IJobState
When a job is executed it is passed an `IJobState` and a `CancellationToken`.  The latter of these should be respected to allow for easy termination of overdue jobs.  The first allows the execution function access to lots of useful information about the current execution, and allows the job to disable itself, preventing further execution.

```csharp
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
    ///     An optional job name.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     The <see cref="IScheduler">scheduler</see> executing this job.
    /// </summary>
    IScheduler Scheduler { get; }

    /// <summary>
    ///     The <see cref="ISchedule">schedule</see> that triggered this execution.
    /// </summary>
    /// <remarks>Will be <c>null</c> if the job was <see cref="IsManual">executed manually</see>.</remarks>
    ISchedule? Schedule { get; }

    /// <summary>
    ///     The <see cref="Instant">instant</see> the job was due to run.
    /// </summary>
    /// <remarks>This will be when the job was requested.</remarks>
    ZonedDateTime Due { get; }

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
```

### IScheduledJob
Similarly, when a job is added to the `IScheduler` it returns an `IScheduledJob`. This is almost identical to `IJobState`, also allowing control over whether the job is enabled, but also allowing for manual execution of the job.

**Note:** A job will never be executed _concurrently_ with itself.  If a job is executed manually, whilst it is also executing as part of a schedule, the manual execution will receive the same task, and vice-versa.  It is effectively 'debounced', meaning that a job execution is inherently thread-safe.

# TODO

* More documentation, and examples.
* Serialization of state for.
* More tests.

## Testing status

* There are some basic unit tests in the `DevDecoder.Scheduling.Test` project.

# Acknowledgements

* https://github.com/webappsuk/CoreLibraries/tree/master/Scheduling - The original library which I created whilst working at Web Applications UK and _inspired_ this work, but is now largely abandoned.  However, this project was built from the ground up to take advantage of the many changes to the wider eco-system in the last decade.
* https://github.com/HangfireIO/Cronos - Cronos is used to parse Cron expressions.
* https://github.com/nodatime/nodatime - The definitive answer to accurate dates and times in .NET!
