using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Services
{
    /// <summary>Outcome of applying an App Store transaction to the saved ledger. Finish the transaction
    /// for Granted, AlreadyGranted and Refunded; leave it unfinished for Rejected and NotSaved.</summary>
    public enum ClinicPurchaseGrant { Granted, AlreadyGranted, Rejected, NotSaved, Refunded }

    /// <summary>Progress and accounted time commit together before offline results are exposed.</summary>
    public sealed class ClinicProfileStore
    {
        public const double MaximumOfflineSeconds = ClinicRules.MaximumOfflineSeconds;
        public const string FileName = "idle-clinic-profile-v1.json";
        private const string LegacyFileName = "little-lifeline-profile-v1.json";
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload; public string checksum; }
        [Serializable] private sealed class LegacyPreferencesProfile { public int schemaVersion = 0; public ClinicPreferences preferences = new ClinicPreferences(); }
        private readonly string directory;
        private readonly string path;
        private bool primaryWasUnreadable;
        private bool migrationPending;
        private string migrationSourcePath;
        private int migrationSourceVersion;
        private string migrationSourceSnapshot;
        private long pendingOfflineUtcTicks;
        public ClinicProfile Profile { get; private set; }
        public ClinicOfflineReport LastOfflineReport { get; private set; } = new ClinicOfflineReport();
        public string Error { get; private set; }
        public bool HasPendingOfflineProgress { get; private set; }

        public ClinicProfileStore(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A save directory is required.", nameof(directory));
            this.directory = directory;
            path = Path.Combine(directory, FileName);
        }

        public ClinicProfile Load(DateTimeOffset now) => LoadClinic(now);

        public ClinicProfile LoadClinic(DateTimeOffset now)
        {
            Error = null;
            LastOfflineReport = new ClinicOfflineReport();
            HasPendingOfflineProgress = false;
            primaryWasUnreadable = false;
            migrationPending = false;
            migrationSourcePath = null;
            migrationSourceSnapshot = null;
            pendingOfflineUtcTicks = 0;
            string recovery = null;
            if (TryRead(path, out var saved, out var migrated))
            {
                Profile = saved;
                if (migrated) { migrationSourcePath = path; migrationSourceVersion = ReadSchema(path); }
            }
            else
            {
                primaryWasUnreadable = File.Exists(path);
                if (primaryWasUnreadable) PreserveUnreadable(path);
                if (TryRead(path + ".backup", out saved, out migrated))
                {
                    Profile = saved;
                    if (migrated) { migrationSourcePath = path + ".backup"; migrationSourceVersion = ReadSchema(migrationSourcePath); }
                    recovery = "Your clinic was recovered from its last good backup. The unreadable save is preserved.";
                }
                else
                {
                    if (File.Exists(path + ".backup")) PreserveUnreadable(path + ".backup");
                    if (primaryWasUnreadable || File.Exists(path + ".backup"))
                        recovery = "Your clinic save could not be read. A fresh clinic is ready; the original files are preserved.";
                    Profile = new ClinicProfile
                    {
                        state = ClinicSimulation.CreateNew().State,
                        preferences = ReadLegacyPreferences(),
                        lastAccountedUtcTicks = now.UtcDateTime.Ticks
                    };
                    Profile.premium.Earn(NewPlayerGems);
                    ClinicSimulation.TryGrantReward(Profile.state, NewPlayerCoins);
                    Save(Profile, now);
                }
            }
            if (migrated)
            {
                migrationPending = true;
                migrationSourceSnapshot = JsonUtility.ToJson(Profile);
                HasPendingOfflineProgress = true;
                pendingOfflineUtcTicks = now.UtcDateTime.Ticks;
            }
            ApplyOffline(now);
            if (Error == null) Error = recovery;
            return Profile;
        }

        /// <summary>Gems a brand-new clinic starts with, recorded as earned so the ledger stays balanced.</summary>
        public const long NewPlayerGems = 5;
        /// <summary>Coins a brand-new clinic starts with, booked as a reward so totals stay consistent.</summary>
        public const long NewPlayerCoins = 250;

        /// <summary>Call after active ticks/actions and before backgrounding. A stale snapshot cannot replace a resumed clinic.</summary>
        public bool Save(ClinicProfile profile, DateTimeOffset now)
        {
            if (HasPendingOfflineProgress) return false;
            if (!IsValid(profile) || (Profile != null && !ReferenceEquals(profile, Profile)))
            {
                Error = "This clinic state could not be saved. The last good save is unchanged.";
                return false;
            }
            ClinicStateMigration.TryAdoptCurrentRules(profile.state);
            ClinicStateMigration.TryAdoptCurrentRules(profile.doctorsState);
            // Active ticks have already happened even if storage fails. Pair their in-memory
            // watermark with that state; the disk snapshot retains its own previous time.
            profile.lastAccountedUtcTicks = Math.Max(profile.lastAccountedUtcTicks, now.UtcDateTime.Ticks);
            profile.Normalize();
            Profile = profile;
            var candidate = Copy(profile);
            candidate.revision = NextRevision(profile.revision);
            if (!WriteSnapshot(candidate)) return false;
            profile.revision = candidate.revision;
            return true;
        }

        /// <summary>Publish travel only after both clinics and the shared wallet commit together.</summary>
        public bool SelectLocation(ClinicLocation destination, DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            if (!Enum.IsDefined(typeof(ClinicLocation), destination)) { Error = "Choose a clinic."; return false; }
            if (destination == Profile.activeLocation) return true;
            if (Profile.doctorsState == null) { Error = "The doctors’ clinic is still locked."; return false; }
            var candidate = Copy(Profile);
            var from = new ClinicSimulation(candidate.ActiveState);
            var to = new ClinicSimulation(destination == ClinicLocation.StarterClinic ? candidate.state : candidate.doctorsState);
            var transferred = from.TransferWalletTo(to);
            if (!transferred.Success) { Error = transferred.Message; return false; }
            candidate.activeLocation = destination;
            return CommitCandidate(candidate, now);
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Development-build QA only: open the doctors clinic without its requirements, for captures.</summary>
        public bool QaOpenDoctorsClinic(DateTimeOffset now)
        {
            if (!CanCommit() || Profile.doctorsState != null) return false;
            var candidate = Copy(Profile);
            candidate.doctorsState = ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic, candidate.state.Seed).State;
            candidate.activeLocation = ClinicLocation.DoctorsClinic;
            return CommitCandidate(candidate, now);
        }
#endif

        public bool OpenDoctorsClinic(DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            if (Profile.doctorsState != null || Profile.activeLocation != ClinicLocation.StarterClinic)
            { Error = "The doctors’ clinic is already open."; return false; }
            var candidate = Copy(Profile);
            var starter = new ClinicSimulation(candidate.state);
            var unlocked = starter.UnlockDoctorsClinic();
            if (!unlocked.Success) { Error = unlocked.Message; return false; }
            var doctors = ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic, candidate.state.Seed);
            var transferred = starter.TransferWalletTo(doctors);
            if (!transferred.Success) { Error = transferred.Message; return false; }
            candidate.doctorsState = doctors.State;
            candidate.activeLocation = ClinicLocation.DoctorsClinic;
            return CommitCandidate(candidate, now);
        }

        /// <summary>Rooms that can build or renovate at once.</summary>
        public int ConstructionSlots => HasUnlock(ClinicUnlocks.ExtraBuilder) ? 2 : 1;
        public bool HasUnlock(string unlock) => Profile?.premium?.unlocks != null && Profile.premium.unlocks.Contains(unlock);

        public bool UnlockExtraBuilderWithGems(DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            if (HasUnlock(ClinicUnlocks.ExtraBuilder)) { Error = "Your second builder is already working for you."; return false; }
            var candidate = Copy(Profile);
            if (!candidate.premium.Spend(ClinicUnlocks.ExtraBuilderGems)) { Error = "You need " + ClinicUnlocks.ExtraBuilderGems + " gems for a second builder."; return false; }
            ClinicPremiumState.Record(candidate.premium.unlocks, ClinicUnlocks.ExtraBuilder);
            return CommitCandidate(candidate, now);
        }

        /// <summary>Mirror an owned App Store unlock into the save, so it works offline. Repeating is harmless.</summary>
        public bool RecordPurchasedUnlock(string unlock, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(unlock)) return false;
            if (HasUnlock(unlock)) return true;
            if (!CanCommit()) return false;
            var candidate = Copy(Profile);
            ClinicPremiumState.Record(candidate.premium.unlocks, unlock);
            return CommitCandidate(candidate, now);
        }

        public IEnumerable<ClinicMilestone> ClaimableMilestones()
            => Profile == null ? Enumerable.Empty<ClinicMilestone>()
                : ClinicMilestones.All.Where(m => !Profile.premium.milestonesClaimed.Contains(m.Id) && m.IsMet(Profile.state, Profile.doctorsState));

        public bool ClaimMilestone(string id, DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            var milestone = ClinicMilestones.Find(id);
            if (milestone == null) { Error = "That goal is not available."; return false; }
            if (Profile.premium.milestonesClaimed.Contains(id)) { Error = "That reward has already been collected."; return false; }
            if (!milestone.IsMet(Profile.state, Profile.doctorsState)) { Error = "Finish the goal to collect its reward."; return false; }
            var candidate = Copy(Profile);
            if (!candidate.premium.Earn(milestone.GemReward)) { Error = "Your gem balance is full."; return false; }
            ClinicPremiumState.Record(candidate.premium.milestonesClaimed, id);
            return CommitCandidate(candidate, now);
        }

        public ClinicGuideProgress GuideProgress => Profile?.premium == null ? default
            : new ClinicGuideProgress(Profile.premium.milestonesClaimed.Count(id => !id.StartsWith(ClinicGuide.Prefix, StringComparison.Ordinal)), Profile.premium.gemsSpent);

        /// <summary>The first unclaimed guide step, done or not; null once the whole guide is collected.</summary>
        public ClinicGuideStep CurrentGuideStep()
            => Profile?.premium == null ? null : ClinicGuide.Steps.FirstOrDefault(s => !Profile.premium.milestonesClaimed.Contains(s.Id));

        public bool IsGuideStepDone(ClinicGuideStep step)
            => step != null && Profile != null && step.IsDone(Profile.state, Profile.doctorsState, GuideProgress);

        public bool ClaimGuideStep(string id, DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            var step = ClinicGuide.Find(id);
            if (step == null) { Error = "That step is not available."; return false; }
            if (Profile.premium.milestonesClaimed.Contains(id)) { Error = "That reward has already been collected."; return false; }
            if (!IsGuideStepDone(step)) { Error = "Finish this step to collect its reward."; return false; }
            var candidate = Copy(Profile);
            if (!candidate.premium.Earn(step.GemReward)) { Error = "Your gem balance is full."; return false; }
            ClinicPremiumState.Record(candidate.premium.milestonesClaimed, id);
            return CommitCandidate(candidate, now);
        }

        /// <summary>Finish active-clinic construction for gems priced from the time actually remaining.</summary>
        public bool SkipConstruction(int jobId, DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            var candidate = Copy(Profile);
            var state = candidate.ActiveState;
            var job = state.Construction.Find(c => c.Id == jobId);
            if (job == null) { Error = "That work has already finished."; return false; }
            var cost = ClinicPremiumRules.SkipCost(state, job);
            if (!candidate.premium.Spend(cost)) { Error = "You need " + cost + " gems to finish this now."; return false; }
            if (!new ClinicSimulation(state).CompleteConstructionNow(jobId).Success) { Error = "That work has already finished."; return false; }
            return CommitCandidate(candidate, now);
        }

        /// <summary>Apply one verified App Store transaction at most once. Finish the transaction only after
        /// <see cref="ClinicPurchaseGrant.Granted"/> or <see cref="ClinicPurchaseGrant.AlreadyGranted"/>.</summary>
        public ClinicPurchaseGrant GrantPurchase(string transactionId, long gems, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(transactionId) || transactionId.Length > ClinicPremiumState.MaximumIdLength || gems <= 0)
                return ClinicPurchaseGrant.Rejected;
            if (Profile?.premium != null && Profile.premium.processedTransactionIds.Contains(transactionId)) return ClinicPurchaseGrant.AlreadyGranted;
            if (Profile?.premium != null && Profile.premium.revokedTransactionIds.Contains(transactionId)) return ClinicPurchaseGrant.Refunded;
            if (!CanCommit()) return ClinicPurchaseGrant.NotSaved;
            var candidate = Copy(Profile);
            if (!candidate.premium.Purchase(gems)) { Error = "Your gem balance is full."; return ClinicPurchaseGrant.NotSaved; }
            ClinicPremiumState.Record(candidate.premium.processedTransactionIds, transactionId);
            return CommitCandidate(candidate, now) ? ClinicPurchaseGrant.Granted : ClinicPurchaseGrant.NotSaved;
        }

        /// <summary>Apple refunded a purchase: remove its gems once, never taking the balance below zero.</summary>
        public ClinicPurchaseGrant RevokePurchase(string transactionId, long gems, DateTimeOffset now)
        {
            if (string.IsNullOrEmpty(transactionId) || transactionId.Length > ClinicPremiumState.MaximumIdLength || gems <= 0)
                return ClinicPurchaseGrant.Rejected;
            if (Profile?.premium != null && Profile.premium.revokedTransactionIds.Contains(transactionId)) return ClinicPurchaseGrant.AlreadyGranted;
            if (!CanCommit()) return ClinicPurchaseGrant.NotSaved;
            var candidate = Copy(Profile);
            // A purchase that was never granted gives nothing back, but is remembered so it cannot be granted later.
            candidate.premium.Revoke(candidate.premium.processedTransactionIds.Contains(transactionId) ? gems : 0);
            ClinicPremiumState.Record(candidate.premium.revokedTransactionIds, transactionId);
            return CommitCandidate(candidate, now) ? ClinicPurchaseGrant.Granted : ClinicPurchaseGrant.NotSaved;
        }

        // ---- Offline limits -------------------------------------------------------------------------------
        public int OfflineLimitHours
        {
            get { var hours = 8; foreach (var tier in ClinicShopOffers.OfflineTiers) if (HasUnlock(tier.Id)) hours = Math.Max(hours, tier.Hours); return hours; }
        }
        public double OfflineCoinMultiplier
        {
            get { var multiplier = 1d; foreach (var tier in ClinicShopOffers.OfflineTiers) if (HasUnlock(tier.Id)) multiplier = Math.Max(multiplier, tier.CoinMultiplier); return multiplier; }
        }
        public long OfflineCoinCap => Profile == null ? 0 : ClinicRules.OfflineCoinCap(Profile.ActiveState, OfflineCoinMultiplier);

        public bool BuyOfflineUpgrade(string id, DateTimeOffset now)
        {
            var tier = Array.Find(ClinicShopOffers.OfflineTiers, o => o.Id == id);
            if (tier == null) { Error = "That upgrade is not available."; return false; }
            if (OfflineLimitHours >= tier.Hours) { Error = "You already have this."; return false; }
            return SpendGems(tier.Gems, now, candidate => ClinicPremiumState.Record(candidate.premium.unlocks, tier.Id));
        }

        // ---- Daily goals and login streak ---------------------------------------------------------------
        public int Today(DateTimeOffset now) => ClinicDaily.Day(now);

        /// <summary>Start a new day's goals from the current totals. Kept in memory until the next save.</summary>
        public void EnsureDay(DateTimeOffset now)
        {
            if (Profile == null) return;
            Profile.Normalize();
            var daily = Profile.daily; var today = Today(now);
            if (daily.firstSeenUtcTicks == 0) daily.firstSeenUtcTicks = now.UtcDateTime.Ticks;
            if (daily.day == today) return;
            var totals = ClinicDailyTotals.Of(Profile.state, Profile.doctorsState);
            daily.day = today; daily.claimed.Clear();
            daily.baseTreatments = totals.Treatments; daily.baseCollected = totals.Collected; daily.baseSpent = totals.Spent; daily.baseParking = totals.ParkingFees;
        }

        public IReadOnlyList<ClinicDailyGoal> DailyGoals(DateTimeOffset now)
        {
            EnsureDay(now);
            var active = Profile.ActiveState;
            return ClinicDaily.Goals(Profile.daily.day, active, (active.Amenity(ClinicAmenity.Parking)?.Level ?? 0) > 0 && ClinicRules.ParkingExitFee(active) > 0);
        }
        public ClinicDailyTotals DailyTotals => ClinicDailyTotals.Of(Profile.state, Profile.doctorsState);
        public bool IsDailyClaimed(string id) => Profile?.daily?.claimed.Contains(id) == true;
        public long DailyProgress(ClinicDailyGoal goal) => goal.Progress(DailyTotals, Profile.daily.Baseline);
        public bool IsDailyDone(ClinicDailyGoal goal) => goal.IsDone(DailyTotals, Profile.daily.Baseline);
        public int DailyReady(DateTimeOffset now) => DailyGoals(now).Count(g => IsDailyDone(g) && !IsDailyClaimed(g.Id)) + (LoginRewardReady(now) ? 1 : 0);

        public bool ClaimDailyGoal(string id, DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            var goal = DailyGoals(now).FirstOrDefault(g => g.Id == id);
            if (goal == null) { Error = "That goal is not on today's list."; return false; }
            if (IsDailyClaimed(id)) { Error = "Already collected today."; return false; }
            if (!IsDailyDone(goal)) { Error = "Finish the goal to collect its reward."; return false; }
            var candidate = Copy(Profile);
            candidate.daily.claimed.Add(id);
            var gems = goal.GemReward + (candidate.daily.claimed.Count == ClinicDaily.GoalsPerDay ? ClinicDaily.AllGoalsGemBonus : 0);
            if (!candidate.premium.Earn(gems) || !ClinicSimulation.TryGrantReward(candidate.ActiveState, goal.CoinReward)) { Error = "Your balance is full."; return false; }
            return CommitCandidate(candidate, now);
        }

        public bool LoginRewardReady(DateTimeOffset now) => Profile?.daily != null && Profile.daily.loginDay != Today(now);
        /// <summary>The streak day the next login reward counts as: it grows on consecutive days and restarts after a gap.</summary>
        public int NextStreakDay(DateTimeOffset now)
            => Profile.daily.loginDay == Today(now) - 1 ? Profile.daily.loginStreak + 1 : Profile.daily.loginDay == Today(now) ? Profile.daily.loginStreak : 1;

        public bool ClaimLoginReward(DateTimeOffset now)
        {
            if (!CanCommit()) return false;
            EnsureDay(now);
            if (!LoginRewardReady(now)) { Error = "Come back tomorrow for the next reward."; return false; }
            var candidate = Copy(Profile);
            var streak = NextStreakDay(now);
            candidate.daily.loginDay = Today(now); candidate.daily.loginStreak = streak;
            if (!candidate.premium.Earn(ClinicDaily.StreakReward(streak))
                || !ClinicSimulation.TryGrantReward(candidate.ActiveState, ClinicDaily.StreakCoins(streak, candidate.ActiveState))) { Error = "Your balance is full."; return false; }
            return CommitCandidate(candidate, now);
        }

        // ---- Coin packs and boosts ----------------------------------------------------------------------
        public long CoinPackAmount(ClinicShopOffers.CoinPack pack) => Math.Max(50, ClinicRules.VisitFee(Profile.ActiveState)) * pack.Visits;

        public bool BuyCoinPack(string id, DateTimeOffset now)
        {
            var pack = Array.Find(ClinicShopOffers.CoinPacks, p => p.Id == id);
            if (pack == null) { Error = "That pack is not available."; return false; }
            var coins = CoinPackAmount(pack);
            return SpendGems(pack.Gems, now, candidate => ClinicSimulation.TryGrantReward(candidate.ActiveState, coins));
        }

        public bool BoostActive(DateTimeOffset now) => Profile?.daily != null && Profile.daily.boostEndsUtcTicks > now.UtcDateTime.Ticks;
        public TimeSpan BoostRemaining(DateTimeOffset now) => BoostActive(now) ? TimeSpan.FromTicks(Profile.daily.boostEndsUtcTicks - now.UtcDateTime.Ticks) : TimeSpan.Zero;

        /// <summary>Add one decor level to a room in the clinic being played, paid in gems (rules 5).</summary>
        public bool BuyDecoration(ClinicRoom room, DateTimeOffset now)
        {
            var state = Profile?.ActiveState;
            if (state == null) { Error = "That room could not be decorated."; return false; }
            var gems = ClinicRules.DecorationGemCost(state, room);
            if (gems <= 0) { Error = "This room is fully decorated."; return false; }
            return SpendGems(gems, now, candidate =>
            {
                var result = new ClinicSimulation(candidate.ActiveState).Decorate(room);
                if (!result.Success) Error = result.Message;
                return result.Success;
            });
        }

        public bool BuyBoost(string id, DateTimeOffset now)
        {
            var boost = Array.Find(ClinicShopOffers.Boosts, b => b.Id == id);
            if (boost == null) { Error = "That boost is not available."; return false; }
            return SpendGems(boost.Gems, now, candidate =>
            {
                var from = Math.Max(candidate.daily.boostEndsUtcTicks, now.UtcDateTime.Ticks);
                candidate.daily.boostEndsUtcTicks = from + TimeSpan.FromHours(boost.Hours).Ticks;
                return true;
            });
        }

        /// <summary>While a boost runs, every collection is matched by the same amount again as a reward.</summary>
        public long GrantBoostBonus(long collected, DateTimeOffset now)
        {
            if (collected <= 0 || !BoostActive(now) || !CanCommit()) return 0;
            var candidate = Copy(Profile);
            if (!ClinicSimulation.TryGrantReward(candidate.ActiveState, collected)) return 0;
            return CommitCandidate(candidate, now) ? collected : 0;
        }

        private bool SpendGems(long gems, DateTimeOffset now, Func<ClinicProfile, bool> apply)
        {
            if (!CanCommit()) return false;
            var candidate = Copy(Profile);
            if (!candidate.premium.Spend(gems)) { Error = "You need " + gems + " gems."; return false; }
            if (!apply(candidate)) { Error = "That could not be applied."; return false; }
            return CommitCandidate(candidate, now);
        }
        private bool SpendGems(long gems, DateTimeOffset now, Action<ClinicProfile> apply) => SpendGems(gems, now, candidate => { apply(candidate); return true; });

        private bool CanCommit()
        {
            if (HasPendingOfflineProgress) { Error = "Saving your return first…"; return false; }
            if (!IsValid(Profile)) { Error = "Your clinic could not be checked. The last good save is unchanged."; return false; }
            return true;
        }

        private bool CommitCandidate(ClinicProfile candidate, DateTimeOffset now)
        {
            candidate.lastAccountedUtcTicks = Math.Max(candidate.lastAccountedUtcTicks, now.UtcDateTime.Ticks);
            candidate.revision = NextRevision(candidate.revision);
            if (!WriteSnapshot(candidate)) return false;
            Profile = candidate;
            return true;
        }

        /// <summary>Core caps earnings at eight hours and independently completes construction for the full gap.
        /// Rebind the simulation to Profile.state after success. Pause active ticks/actions while a commit is pending.</summary>
        public ClinicOfflineReport ApplyOffline(DateTimeOffset now)
        {
            var result = new ClinicOfflineReport();
            if (!IsValid(Profile)) return result;
            if (migrationPending && !CommitMigration()) return result;
            var targetTicks = Math.Max(now.UtcDateTime.Ticks, pendingOfflineUtcTicks);
            var elapsed = (targetTicks - Profile.lastAccountedUtcTicks) / (double)TimeSpan.TicksPerSecond;
            if (elapsed <= 0)
            {
                HasPendingOfflineProgress = false;
                pendingOfflineUtcTicks = 0;
                return result;
            }
            var candidate = Copy(Profile);
            var seconds = OfflineLimitHours * 3600d;
            var advanced = new ClinicSimulation(candidate.state).AdvanceOffline(elapsed, seconds, ClinicRules.OfflineCoinCap(candidate.state, OfflineCoinMultiplier));
            var doctors = candidate.doctorsState == null ? null
                : new ClinicSimulation(candidate.doctorsState).AdvanceOffline(elapsed, seconds, ClinicRules.OfflineCoinCap(candidate.doctorsState, OfflineCoinMultiplier));
            candidate.lastAccountedUtcTicks = Math.Max(candidate.lastAccountedUtcTicks, targetTicks);
            candidate.revision = NextRevision(candidate.revision);
            if (!WriteSnapshot(candidate))
            {
                HasPendingOfflineProgress = true;
                pendingOfflineUtcTicks = targetTicks;
                return result;
            }
            HasPendingOfflineProgress = false;
            pendingOfflineUtcTicks = 0;
            Profile = candidate;
            result.elapsedSeconds = elapsed;
            result.earningsSeconds = advanced.EarningsSeconds;
            result.constructionSeconds = advanced.ConstructionSeconds;
            result.paymentsReceived = AddReport(advanced.PaymentsReceived, doctors?.PaymentsReceived ?? 0);
            result.tillEarned = AddReport(advanced.TillEarned, doctors?.TillEarned ?? 0);
            result.treatmentsCompleted = AddReport(advanced.TreatmentsCompleted, doctors?.TreatmentsCompleted ?? 0);
            result.wasCapped = advanced.WasCapped;
            result.coinCapped = advanced.CoinCapped || (doctors?.CoinCapped ?? false);
            result.limitHours = OfflineLimitHours;
            result.applied = true;
            LastOfflineReport = result;
            return result;
        }

        private bool CommitMigration()
        {
            // Do not advance the old watermark while upgrading the schema. Offline operations
            // get their own subsequent revision, so a failed write can never replay either step.
            if (!PreserveLegacyMigration()) return false;
            var candidate = Copy(Profile);
            candidate.revision = NextRevision(candidate.revision);
            if (!WriteSnapshot(candidate)) return false;
            Profile = candidate;
            migrationPending = false;
            migrationSourcePath = null;
            migrationSourceSnapshot = null;
            HasPendingOfflineProgress = false;
            return true;
        }

        private bool PreserveLegacyMigration()
        {
            try
            {
                if (!TryRead(migrationSourcePath, out var source, out var isLegacy) || !isLegacy
                    || source.revision != Profile.revision || JsonUtility.ToJson(source) != migrationSourceSnapshot)
                    throw new InvalidDataException("The legacy snapshot changed before migration.");
                var original = File.ReadAllBytes(migrationSourcePath);
                var archive = path + ".v" + migrationSourceVersion + "-before-migration-" + Profile.revision;
                if (File.Exists(archive))
                {
                    if (Convert.ToBase64String(File.ReadAllBytes(archive)) != Convert.ToBase64String(original))
                        throw new InvalidDataException("The preserved legacy snapshot differs.");
                    return true;
                }
                var temporary = archive + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(original, 0, original.Length); stream.Flush(true); }
                File.Move(temporary, archive);
                return true;
            }
            catch (Exception)
            {
                Error = "Your existing clinic is safe. Free a little storage so its upgrade can be saved.";
                return false;
            }
        }

        private bool WriteSnapshot(ClinicProfile profile)
        {
            try
            {
                if (!IsValid(profile)) throw new InvalidDataException("Invalid clinic state.");
                profile.Normalize();
                Directory.CreateDirectory(directory);
                var payload = JsonUtility.ToJson(profile);
                var envelope = new Envelope { payload = payload, checksum = Digest(payload) };
                var bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(envelope));
                var temporary = path + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    var preserveBackup = primaryWasUnreadable || !TryRead(path, out _);
                    File.Replace(temporary, path, preserveBackup ? UnreadablePath(path) : path + ".backup");
                }
                else File.Move(temporary, path);
                primaryWasUnreadable = false;
                Error = null;
                return true;
            }
            catch (Exception)
            {
                Error = "Your clinic could not be saved. Keep the app open and free a little storage; offline progress has no separate claim to lose.";
                return false;
            }
        }

        private static bool TryRead(string file, out ClinicProfile profile) => TryRead(file, out profile, out _);

        private static bool TryRead(string file, out ClinicProfile profile, out bool migrated)
        {
            profile = null;
            migrated = false;
            if (!TryPayload(file, out var payload)) return false;
            try
            {
                var candidate = new ClinicProfile();
                JsonUtility.FromJsonOverwrite(payload, candidate);
                if (candidate.schemaVersion == 1 || candidate.schemaVersion == 2)
                {
                    var version = candidate.schemaVersion;
                    if (!IsValidHeader(candidate, version) || candidate.doctorsState != null
                        || candidate.activeLocation != ClinicLocation.StarterClinic) return false;
                    if (!(version == 1 ? ClinicStateMigration.TryMigrateV1(candidate.state)
                        : ClinicStateMigration.TryMigrateV2(candidate.state))) return false;
                    candidate.Normalize();
                    candidate.preferences.music = candidate.preferences.sound;
                    candidate.schemaVersion = 3;
                }
                if (candidate.schemaVersion == 3)
                {
                    // Version 3 predates premium currency. Validate the clinics exactly as 3.3 did,
                    // then start an empty ledger: no stored field can carry gems into version 4.
                    if (!IsValidClinics(candidate, 3)) return false;
                    candidate.premium = new ClinicPremiumState();
                    candidate.schemaVersion = ClinicProfile.CurrentSchemaVersion;
                    migrated = true;
                }
                if (!IsValid(candidate)) { migrated = false; return false; }
                candidate.Normalize();
                profile = candidate;
                return true;
            }
            catch (Exception) { return false; }
        }

        // Read a small preference DTO only. Never load/advance/save the previous campaign,
        // import its cash/tutorial state, or treat any local field as a paid entitlement.
        private ClinicPreferences ReadLegacyPreferences()
        {
            foreach (var suffix in new[] { "", ".backup" })
            {
                if (!TryPayload(Path.Combine(directory, LegacyFileName + suffix), out var payload)) continue;
                try
                {
                    var legacy = JsonUtility.FromJson<LegacyPreferencesProfile>(payload);
                    if (legacy == null || legacy.schemaVersion != 1 || legacy.preferences == null) continue;
                    return new ClinicPreferences
                    {
                        sound = legacy.preferences.sound,
                        music = legacy.preferences.sound,
                        haptics = legacy.preferences.haptics,
                        reducedMotion = legacy.preferences.reducedMotion
                    };
                }
                catch (Exception) { }
            }
            return new ClinicPreferences();
        }

        private static bool TryPayload(string file, out string payload)
        {
            payload = null;
            if (!File.Exists(file)) return false;
            try
            {
                if (new FileInfo(file).Length > 4 * 1024 * 1024) return false;
                var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(file));
                if (envelope == null || envelope.schemaVersion != 1 || string.IsNullOrEmpty(envelope.payload)
                    || string.IsNullOrEmpty(envelope.checksum) || Digest(envelope.payload) != envelope.checksum) return false;
                payload = envelope.payload;
                return true;
            }
            catch (Exception) { return false; }
        }

        private static bool IsValidHeader(ClinicProfile profile, int version)
            => profile != null && profile.schemaVersion == version && profile.revision >= 0
                && profile.lastAccountedUtcTicks > 0 && profile.lastAccountedUtcTicks <= DateTime.MaxValue.Ticks && profile.state != null;

        private static bool IsValid(ClinicProfile profile)
            => IsValidClinics(profile, ClinicProfile.CurrentSchemaVersion) && profile.premium != null && profile.premium.IsValid()
                && (profile.daily == null || profile.daily.IsValid());

        private static bool IsValidClinics(ClinicProfile profile, int version)
        {
            if (!IsValidHeader(profile, version) || profile.additionalClinics == null || profile.additionalClinics.Count > 1
                || (profile.additionalClinics.Count == 1 && profile.additionalClinics[0] == null)
                || !ClinicSimulation.IsValidState(profile.state)
                || profile.state.Location != ClinicLocation.StarterClinic
                || !Enum.IsDefined(typeof(ClinicLocation), profile.activeLocation)) return false;
            if (profile.doctorsState == null)
                return !profile.state.DoctorsClinicUnlocked && profile.activeLocation == ClinicLocation.StarterClinic
                    && profile.state.TotalTransferredIn == 0 && profile.state.TotalTransferredOut == 0;
            var doctors = profile.doctorsState;
            return profile.state.DoctorsClinicUnlocked && doctors.Location == ClinicLocation.DoctorsClinic
                && ClinicSimulation.IsValidState(doctors)
                && profile.state.TotalTransferredIn == doctors.TotalTransferredOut
                && profile.state.TotalTransferredOut == doctors.TotalTransferredIn
                && (profile.activeLocation == ClinicLocation.StarterClinic ? doctors.Wallet : profile.state.Wallet) == 0;
        }

        private static int ReadSchema(string file)
        {
            if (!TryPayload(file, out var payload)) return 0;
            return JsonUtility.FromJson<ClinicProfile>(payload)?.schemaVersion ?? 0;
        }
        private static long AddReport(long left, long right) => left > long.MaxValue - right ? long.MaxValue : left + right;

        private static ClinicProfile Copy(ClinicProfile profile)
        {
            var copy = new ClinicProfile();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(profile), copy);
            return copy;
        }

        private static long NextRevision(long revision) => revision == long.MaxValue ? long.MaxValue : revision + 1;
        private static string Digest(string text)
        {
            using (var hash = SHA256.Create())
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        private static string UnreadablePath(string file) => file + ".unreadable-"
            + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N");
        private static void PreserveUnreadable(string file)
        {
            try { File.Copy(file, UnreadablePath(file), false); }
            catch (Exception) { /* If a recovery copy cannot be written, the original remains untouched. */ }
        }
    }
}
