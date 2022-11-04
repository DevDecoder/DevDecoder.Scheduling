﻿// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Schedules;

namespace DevDecoder.Scheduling.Test;

public class ScheduleOptionsFacts
{
    [Theory]
    // No rounding
    [InlineData(ScheduleOptions.None, "1", "2017-10-01 01:45:01.0000001 Z", "2017-10-01 01:45:02.0000001 Z")]

    // Equality so no-rounding
    [InlineData(ScheduleOptions.AlignSeconds, "1", "2017-10-01 01:45:01 Z", "2017-10-01 01:45:02 Z")]
    [InlineData(ScheduleOptions.AlignMinutes, "1:00", "2017-10-01 01:45 Z", "2017-10-01 01:46 Z")]
    [InlineData(ScheduleOptions.AlignHours, "1:00:00", "2017-10-01 01:00 Z", "2017-10-01 02:00 Z")]
    [InlineData(ScheduleOptions.AlignDays, "1:00:00:00", "2017-10-01 00:00 Z", "2017-10-02 00:00 Z")]

    // Round ups
    [InlineData(ScheduleOptions.AlignSeconds, "1", "2017-10-01 01:45:01.0000001 Z", "2017-10-01 01:45:03 Z")]
    [InlineData(ScheduleOptions.AlignMinutes, "1:00", "2017-10-01 01:45:00.0000001 Z", "2017-10-01 01:47 Z")]
    [InlineData(ScheduleOptions.AlignHours, "1:00:00", "2017-10-01 01:00:00.0000001 Z", "2017-10-01 03:00 Z")]
    [InlineData(ScheduleOptions.AlignDays, "1:00:00:00", "2017-10-01 00:00:00.0000001 Z", "2017-10-03 00:00 Z")]

    // Time-zone not affected
    [InlineData(ScheduleOptions.AlignSeconds, "1", "2017-10-01 01:45:01.0000001 +01", "2017-10-01 01:45:03 +01")]
    [InlineData(ScheduleOptions.AlignMinutes, "1:00", "2017-10-01 01:45:00.0000001 +02:00", "2017-10-01 01:47 +02:00")]
    [InlineData(ScheduleOptions.AlignHours, "1:00:00", "2017-10-01 01:00:00.0000001 -03:30", "2017-10-01 03:00 -03:30")]
    [InlineData(ScheduleOptions.AlignDays, "1:00:00:00", "2017-10-01 00:00:00.0000001 +04:00",
        "2017-10-03 00:00 +04:00")]
    public void TestRounding(ScheduleOptions options, string duration, string fromString, string expectedString)
    {
        var d = duration.ToDuration();
        var schedule = new GapSchedule(d, options);
        var from = fromString.ToZonedDateTime();
        var expected = expectedString.ToZonedDateTime();

        var executed = schedule.Next(TestHelpers.DefaultScheduler, from).ApplyOptions(options);

        Assert.Equal(expected, executed);
        Assert.Equal(d, schedule.Duration);
        Assert.Equal(options, schedule.Options);
        Assert.Equal("d", schedule.Name);
    }
}
