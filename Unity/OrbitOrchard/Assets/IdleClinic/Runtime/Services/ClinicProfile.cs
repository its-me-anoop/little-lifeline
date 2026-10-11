using System;
using System.Collections.Generic;
using System.Linq;
using IdleClinic.Core;

namespace IdleClinic.Services
{
    [Serializable]
    public sealed class ClinicPreferences
    {
        public bool sound = true;
        public bool music = true;
        public bool haptics = true;
        public bool reducedMotion;
    }

    /// <summary>Premium currency shared by every clinic. Bought gems never expire; each App Store
    /// transaction is granted once, recorded here, and finished only after this save commits.</summary>
    [Serializable]
    public sealed class ClinicPremiumState
    {
        public const int MaximumRecordedIds = 4096;
        public const int MaximumIdLength = 128;
        public long gems;
        public long gemsEarned;
        public long gemsPurchased;
        public long gemsSpent;
        // Gems removed because Apple refunded their purchase. Never more than the balance held at the time.
        public long gemsRevoked;
        // Oldest first. Only unfinished transactions are redelivered, so the oldest ids may be trimmed.
        public List<string> processedTransactionIds = new List<string>();
        public List<string> revokedTransactionIds = new List<string>();
        public List<string> milestonesClaimed = new List<string>();
        public List<string> decorationsOwned = new List<string>();
        // Permanent upgrades such as a second builder, whether bought with gems or on the App Store.
        public List<string> unlocks = new List<string>();

        public bool IsValid()
        {
            if (!InRange(gems) || !InRange(gemsEarned) || !InRange(gemsPurchased) || !InRange(gemsSpent) || !InRange(gemsRevoked)
                || (decimal)gems != (decimal)gemsEarned + gemsPurchased - gemsSpent - gemsRevoked) return false;
            return ValidIds(processedTransactionIds) && ValidIds(revokedTransactionIds) && ValidIds(milestonesClaimed) && ValidIds(decorationsOwned) && ValidIds(unlocks);
        }

        internal bool Earn(long amount) => Add(ref gemsEarned, amount);
        internal bool Purchase(long amount) => Add(ref gemsPurchased, amount);
        internal bool Spend(long amount)
        {
            if (amount < 0 || amount > gems) return false;
            gemsSpent += amount; gems -= amount;
            return true;
        }
        /// <summary>Remove up to <paramref name="amount"/> gems; any already spent stay spent.</summary>
        internal long Revoke(long amount)
        {
            var removed = Math.Max(0, Math.Min(amount, gems));
            gemsRevoked += removed; gems -= removed;
            return removed;
        }
        internal static void Record(List<string> ids, string id)
        {
            ids.Add(id);
            if (ids.Count > MaximumRecordedIds) ids.RemoveRange(0, ids.Count - MaximumRecordedIds);
        }
        private bool Add(ref long total, long amount)
        {
            if (amount <= 0 || gems > ClinicRules.MaximumCurrency - amount || total > ClinicRules.MaximumCurrency - amount) return false;
            total += amount; gems += amount;
            return true;
        }

        private static bool InRange(long value) => value >= 0 && value <= ClinicRules.MaximumCurrency;
        private static bool ValidIds(List<string> ids)
        {
            if (ids == null || ids.Count > MaximumRecordedIds) return false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids)
                if (string.IsNullOrEmpty(id) || id.Length > MaximumIdLength || !seen.Add(id)) return false;
            return true;
        }
    }

    /// <summary>One of today's goals exactly as it was set when the day began (4.3 build 23 and later).</summary>
    [Serializable]
    public sealed class ClinicDailyGoalRecord
    {
        public string id;
        public int measure;
        public long target, coins, gems, start;

        public bool IsValid() => !string.IsNullOrEmpty(id) && id.Length <= ClinicPremiumState.MaximumIdLength
            && Enum.IsDefined(typeof(ClinicDailyMeasure), measure) && target > 0 && coins >= 0 && gems >= 0 && gems <= 1000 && start >= 0;
        internal static ClinicDailyGoalRecord From(ClinicDailyGoal goal) => new ClinicDailyGoalRecord
            { id = goal.Id, measure = (int)goal.Measure, target = goal.Target, coins = goal.CoinReward, gems = goal.GemReward, start = goal.StartValue };
        internal ClinicDailyGoal ToGoal() => ClinicDaily.Restore(id, (ClinicDailyMeasure)measure, target, coins, gems, start);
    }

    /// <summary>Daily goals, the login streak and time-limited boosts. Days are UTC day numbers.</summary>
    [Serializable]
    public sealed class ClinicDailyState
    {
        public const int MaximumGoals = 8;
        public int day = -1;
        public long baseTreatments, baseCollected, baseSpent, baseParking;
        public List<string> claimed = new List<string>();
        public int loginDay = -1;
        public int loginStreak;
        public long boostEndsUtcTicks;
        public long firstSeenUtcTicks;
        // Fixed when the day begins. Absent in saves before 4.3 (23): today's goals are then set once on load,
        // keeping goals already collected and counting from the stored start-of-day totals.
        public List<ClinicDailyGoalRecord> goals = new List<ClinicDailyGoalRecord>();
        public bool bonusPaid;

        public bool IsValid() => loginStreak >= 0 && boostEndsUtcTicks >= 0 && firstSeenUtcTicks >= 0
            && baseTreatments >= 0 && baseCollected >= 0 && baseSpent >= 0 && baseParking >= 0
            && claimed != null && claimed.Count <= 16 && !claimed.Exists(string.IsNullOrEmpty)
            && (goals == null || goals.Count <= MaximumGoals && goals.All(g => g != null && g.IsValid())
                && goals.Select(g => g.id).Distinct(StringComparer.Ordinal).Count() == goals.Count);
        internal ClinicDailyTotals Baseline => new ClinicDailyTotals(baseTreatments, baseCollected, baseSpent, baseParking);
    }

    /// <summary>Choices the player made in the guide. Version 1 (4.3 build 23) added optional lessons that can be
    /// skipped and a finish lesson that counts only work finished early. Absent in older saves, which migrate on load.</summary>
    [Serializable]
    public sealed class ClinicGuideState
    {
        public const int CurrentVersion = 1;
        public const int MaximumSkipped = 32;
        public int version;
        /// <summary>Optional lessons the player chose to skip, oldest first. Skipping pays nothing; they can be resumed.</summary>
        public List<string> skipped = new List<string>();
        /// <summary>Construction jobs finished early with Finish (free in the last minute, or with gems).</summary>
        public long workFinishedEarly;
        /// <summary>Set once on migration: the save had already spent gems while the finish lesson counted any spending.</summary>
        public bool legacyGemSpend;

        public bool IsValid() => version >= 0 && workFinishedEarly >= 0 && skipped != null && skipped.Count <= MaximumSkipped
            && skipped.All(id => !string.IsNullOrEmpty(id) && id.Length <= ClinicPremiumState.MaximumIdLength)
            && skipped.Distinct(StringComparer.Ordinal).Count() == skipped.Count;
    }

    [Serializable]
    public sealed class ClinicProfile
    {
        public const int CurrentSchemaVersion = 4;
        public int schemaVersion = CurrentSchemaVersion;
        public long revision;
        public ClinicState state;
        // Unity's inline JSON serializer materializes null custom objects. An empty list
        // explicitly represents a locked location without synthesizing an invalid clinic.
        public List<ClinicState> additionalClinics = new List<ClinicState>();
        public ClinicState doctorsState
        {
            get => additionalClinics != null && additionalClinics.Count == 1 ? additionalClinics[0] : null;
            set { additionalClinics = value == null ? new List<ClinicState>() : new List<ClinicState> { value }; }
        }
        public ClinicLocation activeLocation;
        public ClinicState ActiveState => activeLocation == ClinicLocation.DoctorsClinic ? doctorsState : state;
        public ClinicPreferences preferences = new ClinicPreferences();
        public ClinicPremiumState premium = new ClinicPremiumState();
        public ClinicDailyState daily = new ClinicDailyState();
        public long lastAccountedUtcTicks;
        public ClinicGuideState guide = new ClinicGuideState();

        internal void Normalize()
        {
            if (preferences == null) preferences = new ClinicPreferences();
            if (premium == null) premium = new ClinicPremiumState();
            if (daily == null) daily = new ClinicDailyState();
            if (daily.claimed == null) daily.claimed = new List<string>();
            if (daily.goals == null) daily.goals = new List<ClinicDailyGoalRecord>();
            if (guide == null) guide = new ClinicGuideState();
            if (guide.skipped == null) guide.skipped = new List<string>();
            if (guide.version < ClinicGuideState.CurrentVersion)
            {
                // Before this version the finish lesson completed on any gem spending. A save that had spent gems may
                // already have shown it as done, so it stays collectable; new saves need work actually finished early.
                guide.legacyGemSpend = premium.gemsSpent > 0 && premium.milestonesClaimed?.Contains("guide.finish") != true;
                guide.version = ClinicGuideState.CurrentVersion;
            }
        }
    }

    /// <summary>Already committed results; there is no second claim action.</summary>
    public sealed class ClinicOfflineReport
    {
        public double elapsedSeconds;
        public double earningsSeconds;
        public double constructionSeconds;
        public long paymentsReceived;
        public long tillEarned;
        public long treatmentsCompleted;
        public bool wasCapped;
        /// <summary>Earning stopped early because the tills filled to their offline coin limit.</summary>
        public bool coinCapped;
        public int limitHours = 8;
        public bool applied;
    }
}
