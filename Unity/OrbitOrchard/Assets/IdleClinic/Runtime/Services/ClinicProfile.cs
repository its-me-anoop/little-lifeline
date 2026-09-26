using System;
using System.Collections.Generic;
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

    /// <summary>Daily goals, the login streak and time-limited boosts. Days are UTC day numbers.</summary>
    [Serializable]
    public sealed class ClinicDailyState
    {
        public int day = -1;
        public long baseTreatments, baseCollected, baseSpent, baseParking;
        public List<string> claimed = new List<string>();
        public int loginDay = -1;
        public int loginStreak;
        public long boostEndsUtcTicks;
        public long firstSeenUtcTicks;

        public bool IsValid() => loginStreak >= 0 && boostEndsUtcTicks >= 0 && firstSeenUtcTicks >= 0
            && baseTreatments >= 0 && baseCollected >= 0 && baseSpent >= 0 && baseParking >= 0
            && claimed != null && claimed.Count <= 16 && !claimed.Exists(string.IsNullOrEmpty);
        internal ClinicDailyTotals Baseline => new ClinicDailyTotals(baseTreatments, baseCollected, baseSpent, baseParking);
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

        internal void Normalize()
        {
            if (preferences == null) preferences = new ClinicPreferences();
            if (premium == null) premium = new ClinicPremiumState();
            if (daily == null) daily = new ClinicDailyState();
            if (daily.claimed == null) daily.claimed = new List<string>();
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
