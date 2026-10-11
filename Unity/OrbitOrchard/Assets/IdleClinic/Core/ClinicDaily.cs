using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleClinic.Core
{
    /// <summary>What a daily goal counts. Stored as a number in saves: append only.</summary>
    public enum ClinicDailyMeasure { Treatments, Collected, Spent, ParkingFees, Tips, CheckIns }

    /// <summary>Totals the daily goals measure progress from, summed over both clinics. Every total only grows.</summary>
    public readonly struct ClinicDailyTotals
    {
        public long Treatments { get; }
        public long Collected { get; }
        public long Spent { get; }
        public long ParkingFees { get; }
        public long Tips { get; }
        public long CheckIns { get; }
        public ClinicDailyTotals(long treatments, long collected, long spent, long parkingFees) : this(treatments, collected, spent, parkingFees, 0, 0) { }
        public ClinicDailyTotals(long treatments, long collected, long spent, long parkingFees, long tips, long checkIns)
        { Treatments = treatments; Collected = collected; Spent = spent; ParkingFees = parkingFees; Tips = tips; CheckIns = checkIns; }

        public static ClinicDailyTotals Of(ClinicState starter, ClinicState doctors)
            => new ClinicDailyTotals(Sum(starter, doctors, s => s.TotalTreatments), Sum(starter, doctors, s => s.TotalCollected),
                Sum(starter, doctors, s => s.TotalSpent), Sum(starter, doctors, s => s.TotalParkingFees),
                Sum(starter, doctors, s => s.TotalTips), Sum(starter, doctors, s => s.TotalPayments));

        public long Of(ClinicDailyMeasure measure)
        {
            switch (measure)
            {
                case ClinicDailyMeasure.Treatments: return Treatments;
                case ClinicDailyMeasure.Collected: return Collected;
                case ClinicDailyMeasure.Spent: return Spent;
                case ClinicDailyMeasure.ParkingFees: return ParkingFees;
                case ClinicDailyMeasure.Tips: return Tips;
                default: return CheckIns;
            }
        }

        private static long Sum(ClinicState a, ClinicState b, Func<ClinicState, long> value) => (a == null ? 0 : value(a)) + (b == null ? 0 : value(b));
    }

    /// <summary>One of today's goals. Its target, rewards and starting total are fixed when the day's goals are chosen,
    /// so upgrades, travel between clinics or a relaunch never move them.</summary>
    public sealed class ClinicDailyGoal
    {
        public string Id { get; }
        public string Title { get; }
        public ClinicDailyMeasure Measure { get; }
        public long Target { get; }
        public long CoinReward { get; }
        public long GemReward { get; }
        /// <summary>The measured total when the goal was set; progress counts from here.</summary>
        public long StartValue { get; }
        internal ClinicDailyGoal(string id, string title, ClinicDailyMeasure measure, long target, long coins, long gems, long start = 0)
        { Id = id; Title = title; Measure = measure; Target = target; CoinReward = coins; GemReward = gems; StartValue = start; }
        public long Progress(ClinicDailyTotals now, ClinicDailyTotals start) => Math.Max(0, now.Of(Measure) - start.Of(Measure));
        public bool IsDone(ClinicDailyTotals now, ClinicDailyTotals start) => Progress(now, start) >= Target;
        public long Progress(ClinicDailyTotals now) => Math.Max(0, now.Of(Measure) - StartValue);
        public bool IsDone(ClinicDailyTotals now) => Progress(now) >= Target;
    }

    /// <summary>What the day's goals are chosen from: the clinic being played and what can still be achieved today.</summary>
    public sealed class ClinicDailyContext
    {
        public ClinicState Active;
        public bool HasParking;
        public bool HasVending;
        /// <summary>Coins of improvements still open across both clinics (a lower bound). A spending goal is only set
        /// when it can be met, so a fully upgraded clinic never gets one.</summary>
        public long SpendableCoins = long.MaxValue;
        public ClinicDailyTotals Now;
        /// <summary>Totals when the day began. Goals that existed before this version count from here.</summary>
        public ClinicDailyTotals DayStart;
        /// <summary>Ids already collected today. They are always kept in the plan.</summary>
        public ICollection<string> Keep;
    }

    /// <summary>Three goals every UTC day plus a login streak. Deterministic from the day and clinic seed; targets and
    /// coin rewards scale with the clinic's visit fee when the day's goals are set.</summary>
    public static class ClinicDaily
    {
        public const int GoalsPerDay = 3;
        public const long AllGoalsGemBonus = 10;
        private static readonly long[] StreakGems = { 5, 5, 10, 10, 15, 15, 30 };
        private static readonly Dictionary<string, string> Titles = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["daily.treat"] = "Treat {0} patients",
            ["daily.collect"] = "Collect {0} coins",
            ["daily.spend"] = "Spend {0} coins on improvements",
            ["daily.parking"] = "Earn {0} coins from parking",
            ["daily.tips"] = "Earn {0} coins in vending tips",
            ["daily.checkin"] = "Check in {0} patients",
        };

        public static int Day(DateTimeOffset utc) => (int)(utc.UtcDateTime.Date - new DateTime(2026, 1, 1)).TotalDays;

        /// <summary>The goals as chosen before 4.3 (23): every goal assumed achievable and counted from the day's start.</summary>
        public static IReadOnlyList<ClinicDailyGoal> Goals(int day, ClinicState active, bool hasParking)
            => Plan(day, new ClinicDailyContext { Active = active, HasParking = hasParking });

        /// <summary>Choose the day's three goals. Every goal can be met with ordinary play; collected goals are kept.</summary>
        public static IReadOnlyList<ClinicDailyGoal> Plan(int day, ClinicDailyContext context)
        {
            var active = context.Active;
            var keep = context.Keep ?? (ICollection<string>)new string[0];
            var fee = Math.Max(50, ClinicRules.VisitFee(active));
            var seed = (ulong)(uint)day * 2654435761UL + (active?.Seed ?? 42);
            ClinicDailyGoal Make(string id, ClinicDailyMeasure measure, long target, long coins, long gems, bool countsFromDayStart)
                => new ClinicDailyGoal(id, Titles[id], measure, target, coins, gems, (countsFromDayStart ? context.DayStart : context.Now).Of(measure));

            var treat = Make("daily.treat", ClinicDailyMeasure.Treatments, 40 + (long)(seed % 3) * 20, fee * 20, 3, true);
            var collect = Make("daily.collect", ClinicDailyMeasure.Collected, fee * (40 + (long)(seed / 3 % 3) * 20), fee * 20, 3, true);
            var spend = Make("daily.spend", ClinicDailyMeasure.Spent, fee * (25 + (long)(seed / 9 % 3) * 15), fee * 25, 5, true);
            var parking = Make("daily.parking", ClinicDailyMeasure.ParkingFees,
                Math.Max(30, (active?.Amenity(ClinicAmenity.Parking) == null ? 0 : ClinicRules.ParkingExitFee(active)) * (10 + (long)(seed / 27 % 3) * 5)), fee * 15, 4, true);
            var tips = Make("daily.tips", ClinicDailyMeasure.Tips,
                Math.Max(20, (active?.Amenity(ClinicAmenity.Vending) == null ? 0 : ClinicRules.VendingTip(active)) * (12 + (long)(seed / 243 % 3) * 6)), fee * 10, 3, false);
            var checkIn = Make("daily.checkin", ClinicDailyMeasure.CheckIns, 30 + (long)(seed / 729 % 3) * 15, fee * 15, 3, false);

            var pool = new List<ClinicDailyGoal> { treat, collect };
            if (spend.Target <= context.SpendableCoins || keep.Contains(spend.Id)) pool.Add(spend);
            if (context.HasParking || keep.Contains(parking.Id)) pool.Add(parking);
            // A fully upgraded clinic has nothing left to buy: replace spending with goals ordinary play still meets.
            if (pool.Count < GoalsPerDay && context.HasVending || keep.Contains(tips.Id)) pool.Add(tips);
            if (pool.Count < GoalsPerDay || keep.Contains(checkIn.Id)) pool.Add(checkIn);
            // Drop one goal on parking days so there are always exactly three, never one already collected.
            while (pool.Count > GoalsPerDay)
            {
                var drop = (int)(seed / 81 % (ulong)(pool.Count - 1));
                for (var tries = 0; tries < pool.Count && keep.Contains(pool[drop].Id); tries++) drop = (drop + 1) % pool.Count;
                if (keep.Contains(pool[drop].Id)) break;
                pool.RemoveAt(drop);
            }
            return pool;
        }

        /// <summary>Rebuild a stored goal exactly as it was set.</summary>
        public static ClinicDailyGoal Restore(string id, ClinicDailyMeasure measure, long target, long coins, long gems, long start)
            => new ClinicDailyGoal(id, Titles.TryGetValue(id ?? "", out var title) ? title : "Daily goal ({0})", measure, target, coins, gems, start);

        public static bool IsKnown(string id) => id != null && Titles.ContainsKey(id);

        public static string Describe(ClinicDailyGoal goal) => string.Format(goal.Title, goal.Target.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>Gems for the given streak day (1-based), cycling weekly after the seventh day.</summary>
        public static long StreakReward(int streakDay) => StreakGems[(Math.Max(1, streakDay) - 1) % StreakGems.Length];
        public static long StreakCoins(int streakDay, ClinicState active) => Math.Max(50, ClinicRules.VisitFee(active)) * 5 * Math.Max(1, streakDay % 7 == 0 ? 7 : streakDay % 7);
    }
}
