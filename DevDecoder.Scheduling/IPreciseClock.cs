// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using DevDecoder.Scheduling.Clocks;
using NodaTime;

namespace DevDecoder.Scheduling;

/// <summary>
///     IPreciseClock extends <see cref="IClock" /> to indicate the current <see cref="ClockPrecision">precision</see>.
/// </summary>
public interface IPreciseClock : IClock
{
    /// <summary>
    ///     The clocks <see cref="ClockPrecision">precision</see>.
    /// </summary>
    ClockPrecision Precision { get; }
}
