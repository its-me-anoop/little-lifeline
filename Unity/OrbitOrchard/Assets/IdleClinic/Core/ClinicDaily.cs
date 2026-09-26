using System;
using System.Collections.Generic;

namespace IdleClinic.Core
{
    public enum ClinicDailyMeasure { Treatments, Collected, Spent, ParkingFees }

    /// <summary>Totals the daily goals measure progress from, summed over both clinics.</summary>
    public readonly struct ClinicDailyTotals
    {
        public long Treatments { get; }
        public long Collected { get; }
        public long Spent { get; }
        public long ParkingFees { get; }
        public ClinicDailyTotals(long treatments, long collected, long spent, long parkingFees)
        { Treatments = treatments; Collected = collected; Spent = spent; ParkingFees = parkingFees; }

        public static ClinicDailyTotals Of(ClinicState starter, ClinicState doctors)
            => new ClinicDailyTotals(Sum(starter, doctors, s => s.TotalTreatments), Sum(starter, doctors, s => s.TotalCollected),
                Sum(starter, doctors, s => s.TotalSpent), Sum(starter, doctors, s => s.TotalParkingFees));

        public long Of(ClinicDailyMeasure measure) => measure == ClinicDailyMeasure.Treatments ? Treatments
            : measure == ClinicDailyMeasure.Collected ? Collected : measure == ClinicDailyMeasure.Spent ? Spent : ParkingFees;

        private static long Sum(ClinicState a, ClinicState b, Func<ClinicState, long> value) => (a == null ? 0 : value(a)) + (b == null ? 0 : value(b));
    }

    public sealed class ClinicDailyGoal
    {
        public string Id { get; }
        public string Title { get; }
        public ClinicDailyMeasure Measure { get; }
        public long Target { get; }
        public long CoinReward { get; }
        public long GemReward { get; }
        internal ClinicDailyGoal(string id, string title, ClinicDailyMeasure measure, long target, long coins, long gems)
        { Id = id; Title = title; Measure = measure; Target = target; CoinReward = coins; GemReward = gems; }
        public long Progress(ClinicDailyTotals now, ClinicDailyTotals start) => Math.Max(0, now.Of(Measure) - start.Of(Measure));
        public bool IsDone(ClinicDailyTotals now, ClinicDailyTotals start) => Progress(now, start) >= Target;
    }

    /// <summary>Three fresh goals every UTC day plus a login streak. Deterministic from the day and clinic seed;
    /// targets and coin rewards scale with the clinic's visit fee so they stay meaningful at every level.</summary>
    public static class ClinicDaily
    {
        public const int GoalsPerDay = 3;
        public const long AllGoalsGemBonus = 10;
        private static readonly long[] StreakGems = { 5, 5, 10, 10, 15, 15, 30 };

        public static int Day(DateTimeOffset utc) => (int)(utc.UtcDateTime.Date - new DateTime(2026, 1, 1)).TotalDays;

        public static IReadOnlyList<ClinicDailyGoal> Goals(int day, ClinicState active, bool hasParking)
        {
            var fee = Math.Max(50, ClinicRules.VisitFee(active));
            var seed = (ulong)(uint)day * 2654435761UL + (active?.Seed ?? 42);
            var pool = new List<ClinicDailyGoal>
            {
                new ClinicDailyGoal("daily.treat", "Treat {0} patients", ClinicDailyMeasure.Treatments, 40 + (long)(seed % 3) * 20, fee * 20, 3),
                new ClinicDailyGoal("daily.collect", "Collect {0} coins", ClinicDailyMeasure.Collected, fee * (40 + (long)(seed / 3 % 3) * 20), fee * 20, 3),
                new ClinicDailyGoal("daily.spend", "Spend {0} coins on improvements", ClinicDailyMeasure.Spent, fee * (25 + (long)(seed / 9 % 3) * 15), fee * 25, 5),
            };
            if (hasParking) pool.Add(new ClinicDailyGoal("daily.parking", "Earn {0} coins from parking", ClinicDailyMeasure.ParkingFees,
                Math.Max(30, ClinicRules.ParkingExitFee(active) * (10 + (long)(seed / 27 % 3) * 5)), fee * 15, 4));
            // Drop one goal on parking days so there are always exactly three.
            if (pool.Count > GoalsPerDay) pool.RemoveAt((int)(seed / 81 % (ulong)(pool.Count - 1)));
            return pool;
        }

        public static string Describe(ClinicDailyGoal goal) => string.Format(goal.Title, goal.Target.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Gems for the given streak day (1-based), cycling weekly after the seventh day.</summary>
        public static long StreakReward(int streakDay) => StreakGems[(Math.Max(1, streakDay) - 1) % StreakGems.Length];
        public static long StreakCoins(int streakDay, ClinicState active) => Math.Max(50, ClinicRules.VisitFee(active)) * 5 * Math.Max(1, streakDay % 7 == 0 ? 7 : streakDay % 7);
    }
}
