[![Publish](https://github.com/DevDecoder/DevDecoder.Scheduling/workflows/Build%20and%20Publish/badge.svg)](https://github.com/DevDecoder/DevDecoder.Scheduling/actions?query=workflow%3A%22Build+and+Publish%22)
[![Nuget](https://img.shields.io/nuget/v/DevDecoder.Scheduling)](https://www.nuget.org/packages/DevDecoder.Scheduling/)

# Description
This library provides a cross-platform service easily creating and running tasks on a schedule.

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

TODO

## Scheduler
TODO

### Logging
The `Scheduler` constructor accepts an [`ILogger<Scheduler>`](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger-1?view=dotnet-plat-ext-3.1&viewFallbackFrom=netstandard-2.1) for logging, this is normally injected via dependency injection, but here is an example of a [simple console logger](https://github.com/DevDecoder/HIDDevices/blob/master/HIDDevices.Sample/SimpleConsoleLogger.cs).


# TODO

* More documentation, examples

## Testing status

TODO

# Acknowledgements

* https://github.com/webappsuk/CoreLibraries/tree/master/Scheduling - The original library which I created whilst working at Web Applications UK and _inspired_ this work, but is now largely abandoned.  However, this project was built from the ground up to take advantage of the many changes to the wider eco-system in the last decade.
* https://github.com/HangfireIO/Cronos - Cronos is used to parse Cron expressions.
* https://github.com/nodatime/nodatime - The definitive answer to accurate dates and times in .NET!
