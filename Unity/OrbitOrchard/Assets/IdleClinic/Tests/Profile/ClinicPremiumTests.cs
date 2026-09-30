using System;
using System.IO;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicPremiumTests
    {
        private string directory, path;
        private DateTimeOffset now;
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload, checksum; }

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-premium-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            // Actual installed 3.2(16) campaign: 4,966 treatments and an unrenovated first aid room.
            var fixture = File.ReadAllText(Path.Combine(Application.dataPath, "IdleClinic/Tests/Profile/Fixtures/clinic-3.2.json"));
            File.WriteAllText(path, fixture);
            now = new DateTimeOffset(JsonUtility.FromJson<ClinicProfile>(JsonUtility.FromJson<Envelope>(fixture).payload).lastAccountedUtcTicks, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void SkipPricesAreFreeForAMinuteThenGrowSlowerThanTime()
        {
            Assert.That(ClinicPremiumRules.SkipCost(0), Is.Zero);
            Assert.That(ClinicPremiumRules.SkipCost(60), Is.Zero);
            Assert.That(ClinicPremiumRules.SkipCost(61), Is.EqualTo(3));
            Assert.That(ClinicPremiumRules.SkipCost(180), Is.EqualTo(5));
            Assert.That(ClinicPremiumRules.SkipCost(3600), Is.EqualTo(44));
            Assert.That(ClinicPremiumRules.SkipCost(double.NaN), Is.Zero);
            Assert.That(ClinicPremiumRules.SkipCost(7200) / 7200d, Is.LessThan(ClinicPremiumRules.SkipCost(3600) / 3600d));
        }

        [Test] public void MilestoneIdsAreUniqueStorableAndRewarded()
        {
            var ids = ClinicMilestones.All.Select(m => m.Id).ToList();
            Assert.That(ids, Is.Unique);
            Assert.That(ids.All(id => id.Length > 0 && id.Length <= ClinicPremiumState.MaximumIdLength), Is.True);
            Assert.That(ClinicMilestones.All.All(m => m.GemReward > 0 && !string.IsNullOrEmpty(m.Title)), Is.True);
            var fresh = ClinicSimulation.CreateNew().State;
            Assert.That(ClinicMilestones.All.Where(m => m.IsMet(fresh, null)), Is.Empty, "A new player has earned nothing yet.");
        }

        [Test] public void ExistingPlayersCanClaimWhatTheyAlreadyAchievedExactlyOnce()
        {
            var store = new ClinicProfileStore(directory); var profile = store.LoadClinic(now);
            var claimable = store.ClaimableMilestones().Select(m => m.Id).ToList();
            Assert.That(claimable, Does.Contain("care.first").And.Contain("care.1000").And.Contain("earned.100k").And.Contain("waiting.built"));
            Assert.That(claimable, Does.Not.Contain("care.10000").And.Not.Contain("doctors.open"));
            var wallet = profile.state.Wallet;

            Assert.That(store.ClaimMilestone("care.first", now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(5));
            Assert.That(store.Profile.premium.gemsEarned, Is.EqualTo(5));
            Assert.That(store.ClaimMilestone("care.first", now), Is.False);
            Assert.That(store.ClaimMilestone("care.10000", now), Is.False, "Unmet goals pay nothing.");
            Assert.That(store.ClaimMilestone("no.such.goal", now), Is.False);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(5));
            Assert.That(store.Profile.state.Wallet, Is.EqualTo(wallet), "Gem rewards never touch coins.");
            Assert.That(store.ClaimableMilestones().Select(m => m.Id), Does.Not.Contain("care.first"));

            var reloaded = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(reloaded.premium.milestonesClaimed, Is.EqualTo(new[] { "care.first" }));
            Assert.That(reloaded.premium.gems, Is.EqualTo(5));
        }

        [Test] public void GemsFinishConstructionWithTheSameResultAsWaiting()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var sim = new ClinicSimulation(store.Profile.state);
            var tier = sim.State.Room(ClinicRoom.FirstAid).Tier;
            Assert.That(sim.Renovate(ClinicRoom.FirstAid).Success, Is.True);
            var job = sim.State.Construction.Single();
            var cost = ClinicPremiumRules.SkipCost(sim.State, job);
            Assert.That(cost, Is.GreaterThan(0));
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);

            Assert.That(store.SkipConstruction(job.Id, now), Is.False, "No gems, no skip.");
            Assert.That(store.Error, Does.Contain(cost + " gems"));
            Assert.That(store.Profile.state.Construction.Count, Is.EqualTo(1));

            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            var coins = store.Profile.state.Wallet;
            Assert.That(store.SkipConstruction(job.Id, now), Is.True, store.Error);
            var state = store.Profile.state;
            Assert.That(state.Construction, Is.Empty);
            Assert.That(state.Room(ClinicRoom.FirstAid).Tier, Is.EqualTo(tier + 1));
            Assert.That(state.Wallet, Is.EqualTo(coins), "Skipping costs gems only.");
            Assert.That(store.Profile.premium.gems, Is.EqualTo(100 - cost));
            Assert.That(store.Profile.premium.gemsSpent, Is.EqualTo(cost));
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
            Assert.That(store.SkipConstruction(job.Id, now), Is.False, "Finished work cannot be bought twice.");
            Assert.That(JsonUtility.ToJson(new ClinicProfileStore(directory).LoadClinic(now)), Is.EqualTo(JsonUtility.ToJson(store.Profile)));
        }

        [Test] public void EachTransactionGrantsOnceAndAFailedSaveGrantsNothingUntilRetried()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            Assert.That(store.GrantPurchase("", 100, now), Is.EqualTo(ClinicPurchaseGrant.Rejected));
            Assert.That(store.GrantPurchase("txn-1", 0, now), Is.EqualTo(ClinicPurchaseGrant.Rejected));

            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.NotSaved), "Do not finish: StoreKit must redeliver.");
            Assert.That(store.Profile.premium.gems, Is.Zero);
            Directory.Delete(path + ".tmp");

            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.AlreadyGranted));
            var reopened = new ClinicProfileStore(directory); reopened.LoadClinic(now);
            Assert.That(reopened.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.AlreadyGranted), "Redelivery after relaunch.");
            Assert.That(reopened.Profile.premium.gems, Is.EqualTo(100));
            Assert.That(reopened.Profile.premium.gemsPurchased, Is.EqualTo(100));
        }

        [Test] public void RefundsRemoveUnspentGemsOnceAndBlockLateGrants()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            var sim = new ClinicSimulation(store.Profile.state);
            Assert.That(sim.Renovate(ClinicRoom.FirstAid).Success, Is.True);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(store.SkipConstruction(sim.State.Construction.Single().Id, now), Is.True, store.Error);
            var spent = store.Profile.premium.gemsSpent;

            Assert.That(store.RevokePurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            var premium = store.Profile.premium;
            Assert.That(premium.gems, Is.Zero);
            Assert.That(premium.gemsRevoked, Is.EqualTo(100 - spent), "Spent gems stay spent; the balance never goes negative.");
            Assert.That(premium.IsValid(), Is.True);
            Assert.That(store.RevokePurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.AlreadyGranted));

            Assert.That(store.RevokePurchase("txn-2", 50, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            Assert.That(store.Profile.premium.gemsRevoked, Is.EqualTo(100 - spent), "A never-granted purchase removes nothing.");
            Assert.That(store.GrantPurchase("txn-2", 50, now), Is.EqualTo(ClinicPurchaseGrant.Refunded), "A refunded purchase cannot be granted later.");
            Assert.That(store.Profile.premium.gems, Is.Zero);
        }

        [Test] public void LiveSavesAdoptRulesFourOnlyAfterTheirConstructionFinishes()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var sim = new ClinicSimulation(store.Profile.state);
            Assert.That(sim.State.RulesVersion, Is.EqualTo(3), "Loading alone changes no prices.");
            string Levels(ClinicState s) => string.Join(",", s.Rooms.Select(r => r.Tier + ":" + r.EquipmentLevel + ":" + r.FacilitiesLevel + ":" + r.DecorationLevel));

            Assert.That(sim.Renovate(ClinicRoom.FirstAid).Success, Is.True);
            var job = sim.State.Construction.Single();
            Assert.That(job.EndsTick - job.StartedTick, Is.EqualTo(1800), "Bought under rules 3: three minutes.");
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(store.Profile.state.RulesVersion, Is.EqualTo(3), "Work in progress keeps the rules it was bought with.");
            Assert.That(ClinicSimulation.IsValidState(store.Profile.state), Is.True);

            sim.Advance(180);
            Assert.That(sim.State.Construction, Is.Empty);
            var wallet = sim.State.Wallet;
            var levels = Levels(sim.State);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            var adopted = store.Profile.state;
            Assert.That(adopted.RulesVersion, Is.EqualTo(ClinicBalance.CurrentRulesVersion));
            Assert.That(ClinicSimulation.IsValidState(adopted), Is.True);
            Assert.That(adopted.Wallet, Is.EqualTo(wallet), "Adoption never charges anything.");
            Assert.That(Levels(adopted), Is.EqualTo(levels), "Owned levels are untouched.");

            Assert.That(sim.Renovate(ClinicRoom.Waiting).Success, Is.True);
            var next = sim.State.Construction.Single();
            Assert.That(next.PaidCost, Is.EqualTo(ClinicRules.RenovationCost(adopted, ClinicRoom.Waiting)));
            Assert.That(next.EndsTick - next.StartedTick, Is.EqualTo(ClinicRules.RenovationSeconds(adopted, ClinicRoom.Waiting) * 10L), "New work uses the current rules.");
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(JsonUtility.ToJson(new ClinicProfileStore(directory).LoadClinic(now).state), Is.EqualTo(JsonUtility.ToJson(store.Profile.state)));
        }

        [Test] public void GuideStepsPayOnceInOrderAndOnlyWhenDone()
        {
            var ids = ClinicGuide.Steps.Select(s => s.Id).ToList();
            Assert.That(ids, Is.Unique);
            Assert.That(ids.All(id => id.StartsWith(ClinicGuide.Prefix) && ClinicMilestones.Find(id) == null), Is.True, "Guide ids never collide with goals.");

            // The genuine 3.2 campaign has already done the first steps: collect them in order.
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var first = store.CurrentGuideStep();
            Assert.That(first.Id, Is.EqualTo("guide.equipment"));
            Assert.That(store.IsGuideStepDone(first), Is.True);
            Assert.That(store.ClaimGuideStep(first.Id, now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(first.GemReward));
            Assert.That(store.ClaimGuideStep(first.Id, now), Is.False, "Each step pays once.");
            Assert.That(store.CurrentGuideStep().Id, Is.EqualTo("guide.waiting"));
            Assert.That(store.ClaimableMilestones().Any(m => m.Id.StartsWith(ClinicGuide.Prefix)), Is.False);

            // A step that is not done pays nothing and stays current.
            var goals = ClinicGuide.Find("guide.goals");
            Assert.That(store.IsGuideStepDone(goals), Is.False, "No goal has been claimed yet.");
            Assert.That(store.ClaimGuideStep(goals.Id, now), Is.False);
            Assert.That(store.ClaimMilestone("care.first", now), Is.True, store.Error);
            Assert.That(store.GuideProgress.GoalsClaimed, Is.EqualTo(1), "Guide steps are not counted as goals.");
            Assert.That(store.IsGuideStepDone(goals), Is.True);

            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            Assert.That(reloaded.CurrentGuideStep().Id, Is.EqualTo("guide.waiting"));
        }

        [Test] public void SecondBuilderUnlocksOnceByGemsOrPurchaseAndSurvivesReload()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            Assert.That(store.ConstructionSlots, Is.EqualTo(1));
            Assert.That(store.UnlockExtraBuilderWithGems(now), Is.False, "No gems yet.");
            Assert.That(store.Error, Does.Contain(ClinicUnlocks.ExtraBuilderGems + " gems"));
            Assert.That(store.GrantPurchase("txn-1", 450, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            Assert.That(store.UnlockExtraBuilderWithGems(now), Is.True, store.Error);
            Assert.That(store.ConstructionSlots, Is.EqualTo(2));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(450 - ClinicUnlocks.ExtraBuilderGems));
            Assert.That(store.UnlockExtraBuilderWithGems(now), Is.False, "Bought once.");
            Assert.That(store.Profile.premium.gems, Is.EqualTo(450 - ClinicUnlocks.ExtraBuilderGems));
            Assert.That(store.RecordPurchasedUnlock(ClinicUnlocks.ExtraBuilder, now), Is.True, "Repeating an owned unlock is harmless.");
            Assert.That(store.Profile.premium.unlocks, Is.EqualTo(new[] { ClinicUnlocks.ExtraBuilder }));
            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            Assert.That(reloaded.ConstructionSlots, Is.EqualTo(2));

            var other = Path.Combine(directory, "purchased"); Directory.CreateDirectory(other);
            var buyer = new ClinicProfileStore(other); buyer.LoadClinic(now);
            Assert.That(buyer.RecordPurchasedUnlock(ClinicUnlocks.ExtraBuilder, now), Is.True, buyer.Error);
            Assert.That(buyer.ConstructionSlots, Is.EqualTo(2));
            Assert.That(buyer.Profile.premium.gems, Is.Zero, "An App Store unlock costs no gems.");
        }

        [Test] public void DailyGoalsPayOnceAndTheLoginStreakGrowsOrRestarts()
        {
            var store = new ClinicProfileStore(directory); var profile = store.LoadClinic(now);
            var goals = store.DailyGoals(now);
            Assert.That(goals.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
            Assert.That(goals.Select(g => g.Id), Is.Unique);
            var first = goals[0];
            Assert.That(store.ClaimDailyGoal(first.Id, now), Is.False, "Nothing done yet today.");
            // Play until the first goal is done.
            var sim = new ClinicSimulation(store.Profile.ActiveState);
            for (var i = 0; i < 600 && !store.IsDailyDone(first); i++)
            {
                sim.Advance(30, false);
                foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id);
                if (first.Measure == ClinicDailyMeasure.Spent) sim.Upgrade(ClinicRoom.Reception, UpgradeTrack.Decoration);
                if (first.Measure == ClinicDailyMeasure.Spent) sim.Upgrade(ClinicRoom.FirstAid, UpgradeTrack.Equipment);
            }
            Assert.That(store.IsDailyDone(first), Is.True, first.Id);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            var wallet = store.Profile.ActiveState.Wallet; var gems = store.Profile.premium.gems;
            Assert.That(store.ClaimDailyGoal(first.Id, now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems + first.GemReward));
            Assert.That(store.Profile.ActiveState.Wallet, Is.EqualTo(wallet + first.CoinReward));
            Assert.That(ClinicSimulation.IsValidState(store.Profile.ActiveState), Is.True);
            Assert.That(store.ClaimDailyGoal(first.Id, now), Is.False, "Once a day.");

            // Streak: day 1 today, day 2 tomorrow, then a missed day restarts it.
            Assert.That(store.NextStreakDay(now), Is.EqualTo(1));
            Assert.That(store.ClaimLoginReward(now), Is.True, store.Error);
            Assert.That(store.ClaimLoginReward(now), Is.False, "Once a day.");
            var tomorrow = now.AddDays(1);
            Assert.That(store.NextStreakDay(tomorrow), Is.EqualTo(2));
            Assert.That(store.ClaimLoginReward(tomorrow), Is.True, store.Error);
            Assert.That(store.Profile.daily.loginStreak, Is.EqualTo(2));
            Assert.That(store.NextStreakDay(now.AddDays(3)), Is.EqualTo(1), "A missed day starts again.");
            Assert.That(store.DailyGoals(tomorrow).Select(g => g.Id), Is.Not.Empty);
            Assert.That(store.IsDailyClaimed(first.Id), Is.False, "A new day brings fresh goals.");
            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(tomorrow);
            Assert.That(reloaded.Profile.daily.loginStreak, Is.EqualTo(2));
        }

        [Test] public void GemsBuyCoinsBoostsAndLongerOfflineEarnings()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            Assert.That(store.GrantPurchase("txn-1", 1000, now), Is.EqualTo(ClinicPurchaseGrant.Granted));
            var pack = ClinicShopOffers.CoinPacks[0]; var wallet = store.Profile.ActiveState.Wallet;
            Assert.That(store.BuyCoinPack(pack.Id, now), Is.True, store.Error);
            Assert.That(store.Profile.ActiveState.Wallet, Is.EqualTo(wallet + store.CoinPackAmount(pack)));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(1000 - pack.Gems));
            Assert.That(ClinicSimulation.IsValidState(store.Profile.ActiveState), Is.True);

            Assert.That(store.GrantBoostBonus(100, now), Is.Zero, "No boost, no bonus.");
            Assert.That(store.BuyBoost("boost.1h", now), Is.True, store.Error);
            Assert.That(store.BoostActive(now.AddMinutes(59)), Is.True);
            Assert.That(store.BoostActive(now.AddMinutes(61)), Is.False);
            wallet = store.Profile.ActiveState.Wallet;
            Assert.That(store.GrantBoostBonus(100, now), Is.EqualTo(100));
            Assert.That(store.Profile.ActiveState.Wallet, Is.EqualTo(wallet + 100));
            Assert.That(store.GrantBoostBonus(100, now.AddHours(2)), Is.Zero);

            // Older saves keep unlimited offline coins until they adopt the current rules on save.
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(store.Profile.ActiveState.RulesVersion, Is.EqualTo(ClinicBalance.CurrentRulesVersion));
            Assert.That(store.OfflineLimitHours, Is.EqualTo(8));
            var cap = store.OfflineCoinCap;
            Assert.That(cap, Is.GreaterThan(0));
            Assert.That(store.BuyOfflineUpgrade("offline.12h", now), Is.True, store.Error);
            Assert.That(store.OfflineLimitHours, Is.EqualTo(12));
            Assert.That(store.OfflineCoinCap, Is.GreaterThan(cap));
            Assert.That(store.BuyOfflineUpgrade("offline.12h", now), Is.False, "Bought once.");
            Assert.That(store.Profile.premium.IsValid(), Is.True);
        }

        [Test] public void GemOperationsWaitForPendingOfflineProgress()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.ApplyOffline(now.AddMinutes(5)).applied, Is.False);
            Assert.That(store.HasPendingOfflineProgress, Is.True);
            Assert.That(store.ClaimMilestone("care.first", now), Is.False);
            Assert.That(store.GrantPurchase("txn-1", 100, now), Is.EqualTo(ClinicPurchaseGrant.NotSaved));
            Directory.Delete(path + ".tmp");
            Assert.That(store.ApplyOffline(now.AddMinutes(5)).applied, Is.True, store.Error);
            Assert.That(store.GrantPurchase("txn-1", 100, now.AddMinutes(5)), Is.EqualTo(ClinicPurchaseGrant.Granted));
        }
    }
}
