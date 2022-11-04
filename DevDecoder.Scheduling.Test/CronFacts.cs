// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Cronos;
using DevDecoder.Scheduling.Clocks;
using DevDecoder.Scheduling.Schedules;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;

namespace DevDecoder.Scheduling.Test
{
    public class CronFacts
    {
        private static readonly bool s_isUnix =
#if NETCOREAPP1_1
            !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
            Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
#endif
        private static readonly string s_lordHoweTimeZoneId = s_isUnix ? "Australia/Lord_Howe" : "Lord Howe Standard Time";
        
        private static readonly IEnumerable<OffsetDateTimePattern> s_offsetDateTimePatterns =
            new[]
            {
                OffsetDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd HH:mm o<G>"),
                OffsetDateTimePattern.CreateWithInvariantCulture("yyyy-MM-dd HH:mm:ss o<G>")
            };

        private static readonly IPreciseClock s_clock = TestClock.From();
        private static readonly IScheduler s_scheduler = new Scheduler(s_clock);


        [Theory]
        // 2017-10-01 is date when the clock jumps forward from 1:59 am +10:30 standard time (ST) to 2:30 am +11:00 DST on Lord Howe.
        // ________1:59 ST///invalid///2:30 DST________
        [InlineData("0 */30 *      *  *  *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 1-58 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 2      01 10 *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 02     01 10 *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 30   2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 30   0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]

        [InlineData("0 0,30,59 *      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30,59 *      *  *  *    ", "2017-10-01 02:30 +11:00", "2017-10-01 02:59 +11:00")]

        [InlineData("0 30   *      *  10 SUN#1", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        public void HandleDST_WhenTheClockTurnedForwardHalfHour(string cronExpression, string fromString, string expectedString)
        {
            var schedule = new CronSchedule(cronExpression, CronFormat.IncludeSeconds);
            var from = GetZonedDateTime(fromString, s_lordHoweTimeZoneId);
            var expected = GetZonedDateTime(expectedString, s_lordHoweTimeZoneId);

            var executed = schedule.Next(s_scheduler, from);

            Assert.Equal(expected, executed);
        }
        
        [Theory]
        // 2017-04-02 is date when the clock jumps backward from 2:00 am -+11:00 DST to 1:30 am +10:30 ST on Lord Howe.
        // _______1:30 DST____1:59 DST -> 1:30 ST____2:00 ST_______

        // Run at 2:00 ST because 2:00 DST is invalid.
        [InlineData("0 */30 */2 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    */2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    0/2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    2-3 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]

        // Run twice due to intervals.
        [InlineData("0 */30 *   * * *", "2017-04-02 01:29:59 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 */30 *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   *   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +10:30")]
        [InlineData("0 30   *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   */1 * * *", "2017-04-02 01:29:59 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   */1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]
        [InlineData("0 30   0/1 * * *", "2017-04-02 01:29:59 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   0/1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   1-9 * * *", "2017-04-02 01:29:59 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   1-9 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 */30 1   * * *", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 */30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 */30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 0/30 1   * * *", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0/30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0/30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 0-30 1   * * *", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0-30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:21 +11:00")]
        [InlineData("0 0-30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("*/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        [InlineData("0/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        [InlineData("0-30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:02 +11:00")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:02 +10:30")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        // Duplicates skipped due to certain time.
        [InlineData("0 0,30 1   * * *", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 00:59:59 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 30    1   * * *", "2017-04-02 01:30 +11:00", "2017-04-03 01:30 +10:30")]
        [InlineData("0 30    1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:30 +10:30")]
        public void HandleDST_WhenTheClockJumpedBackwardAndDeltaIsNotHour(string cronExpression, string fromString, string expectedString)
        {
            var schedule = new CronSchedule(cronExpression, CronFormat.IncludeSeconds);
            var from = GetZonedDateTime(fromString, s_lordHoweTimeZoneId);
            var expected = GetZonedDateTime(expectedString, s_lordHoweTimeZoneId);

            var executed = schedule.Next(s_scheduler, from);

            Assert.Equal(expected, executed);
        }

        private static ZonedDateTime GetZonedDateTime(string offsetDateTime, string timeZoneId)
        {
            offsetDateTime = offsetDateTime.Trim();
            OffsetDateTime? odt = null;
            foreach (var pattern in s_offsetDateTimePatterns)
            {
                var parseResult = pattern.Parse(offsetDateTime);
                if (parseResult.Success)
                {
                    odt = parseResult.Value;
                    break;
                }
            }
            Assert.NotNull(odt);
            return new ZonedDateTime(odt!.Value.LocalDateTime, DateTimeZoneProviders.Bcl[timeZoneId], odt.Value.Offset);
        }
    }
}
