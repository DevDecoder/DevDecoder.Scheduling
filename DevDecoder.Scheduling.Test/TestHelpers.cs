// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using NodaTime;
using NodaTime.Text;

namespace DevDecoder.Scheduling.Test;

public static class TestHelpers
{
    public static readonly bool IsUnix =
#if NETCOREAPP1_1
            !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
        Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
#endif


    public static readonly string EasternTimeZoneId = IsUnix ? "America/New_York" : "Eastern Standard Time";
    public static readonly string JordanTimeZoneId = IsUnix ? "Asia/Amman" : "Jordan Standard Time";
    public static readonly string LordHoweTimeZoneId = IsUnix ? "Australia/Lord_Howe" : "Lord Howe Standard Time";
    public static readonly string PacificTimeZoneId = IsUnix ? "America/Santiago" : "Pacific SA Standard Time";

    public static readonly DateTimeZone EasternTimeZone = DateTimeZoneProviders.Bcl[EasternTimeZoneId];
    public static readonly DateTimeZone JordanTimeZone = DateTimeZoneProviders.Bcl[JordanTimeZoneId];
    public static readonly DateTimeZone LordHoweTimeZone = DateTimeZoneProviders.Bcl[LordHoweTimeZoneId];
    public static readonly DateTimeZone PacificTimeZone = DateTimeZoneProviders.Bcl[PacificTimeZoneId];


    public static readonly IPreciseClock DefaultClock = TestClock.From();
    public static readonly IScheduler DefaultScheduler = new Scheduler(DefaultClock);


    private static readonly IEnumerable<OffsetDateTimePattern> s_offsetDateTimePatterns =
        new[]
        {
            OffsetDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd HH:mm o<G>"),
            OffsetDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd HH:mm:ss.FFFFFFF o<G>")
        };

    private static readonly IEnumerable<DurationPattern> s_durationPatterns =
        new[]
        {
            DurationPattern.CreateWithInvariantCulture("-S.FFFFFFF"),
            DurationPattern.CreateWithInvariantCulture("-M:ss.FFFFFFF"),
            DurationPattern.CreateWithInvariantCulture("-H:mm:ss.FFFFFFF"), DurationPattern.Roundtrip
        };

    private static T Parse<T>(string value, IEnumerable<IPattern<T>> patterns)
        where T : struct, IFormattable
    {
        value = value.Trim();
        T? result = null;
        foreach (var pattern in patterns)
        {
            var parseResult = pattern.Parse(value);
            if (parseResult.Success)
            {
                result = parseResult.Value;
                break;
            }
        }

        Assert.NotNull(result);
        return result!.Value;
    }

    public static Duration ToDuration(this string duration)
        => Parse(duration, s_durationPatterns);

    public static OffsetDateTime ToOffsetDateTime(this string offsetDateTime)
        => Parse(offsetDateTime, s_offsetDateTimePatterns);

    public static ZonedDateTime ToZonedDateTime(this string offsetDateTime, string timeZoneId)
        => ToZonedDateTime(offsetDateTime, DateTimeZoneProviders.Bcl[timeZoneId]);

    public static ZonedDateTime ToZonedDateTime(this string offsetDateTime, DateTimeZone? timeZone = null)
    {
        var odt = Parse(offsetDateTime, s_offsetDateTimePatterns);
        timeZone ??= DateTimeZone.ForOffset(odt.Offset);
        return new ZonedDateTime(odt.LocalDateTime, timeZone, odt.Offset);
    }
}
