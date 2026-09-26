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
        public List<string> processedTransactionIds = new List<string>();
        public List<string> milestonesClaimed = new List<string>();
        public List<string> decorationsOwned = new List<string>();

        public bool IsValid()
        {
            if (!InRange(gems) || !InRange(gemsEarned) || !InRange(gemsPurchased) || !InRange(gemsSpent)
                || (decimal)gems != (decimal)gemsEarned + gemsPurchased - gemsSpent) return false;
            return ValidIds(processedTransactionIds) && ValidIds(milestonesClaimed) && ValidIds(decorationsOwned);
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
        public long lastAccountedUtcTicks;

        internal void Normalize()
        {
            if (preferences == null) preferences = new ClinicPreferences();
            if (premium == null) premium = new ClinicPremiumState();
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
        public bool applied;
    }
}
