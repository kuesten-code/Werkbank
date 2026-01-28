using System;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Provides rounding logic for durations.
/// </summary>
public class TimeRoundingService
{
    public TimeSpan RoundDuration(TimeSpan duration, int roundingMinutes)
    {
        if (roundingMinutes <= 0)
        {
            return duration;
        }

        var totalMinutes = Math.Max(0, duration.TotalMinutes);
        var rounded = Math.Ceiling(totalMinutes / roundingMinutes) * roundingMinutes;
        return TimeSpan.FromMinutes(rounded);
    }
}
