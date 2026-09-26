using System;

namespace IdleClinic.Core
{
    /// <summary>Gem prices. Gems only save time or buy cosmetics; every result is also reachable with coins and patience.</summary>
    public static class ClinicPremiumRules
    {
        /// <summary>Work that finishes within this window is always free to complete.</summary>
        public const double FreeSkipSeconds = 60;
        public const double SkipGemsPerScaledMinute = 2;
        public const double SkipMinuteExponent = 0.75;

        /// <summary>Longer waits cost more in total but less per minute: ceil(2 · minutes^0.75).</summary>
        public static long SkipCost(double remainingSeconds)
        {
            if (double.IsNaN(remainingSeconds) || remainingSeconds <= FreeSkipSeconds) return 0;
            var minutes = remainingSeconds / 60d;
            return (long)Math.Ceiling(SkipGemsPerScaledMinute * Math.Pow(minutes, SkipMinuteExponent));
        }

        public static double RemainingSeconds(ClinicState state, ClinicConstructionState job)
            => state == null || job == null ? 0 : Math.Max(0, job.EndsTick - state.Tick) / (double)ClinicRules.TicksPerSecond;

        public static long SkipCost(ClinicState state, ClinicConstructionState job) => SkipCost(RemainingSeconds(state, job));
    }
}
