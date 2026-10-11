using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>Today's goals are fixed when the day begins: fee upgrades, opening the car park, travel and relaunches never
    /// move a target, reward or the goal list; collected goals and the all-goals bonus pay exactly once.</summary>
    public sealed class ClinicDailyGoalTests
    {
        private string directory, path;
        private DateTimeOffset now;
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload, checksum; }

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-daily-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            now = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        private ClinicProfileStore OpenClinic()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            ClinicGuideSkipTests.FinishOpening(new ClinicSimulation(store.Profile.state));
            ClinicSimulation.TryGrantReward(store.Profile.state, 200000);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            return store;
        }

        private static string Fingerprint(ClinicProfileStore store, DateTimeOffset at)
            => string.Join("|", store.DailyGoals(at).Select(g => g.Id + ":" + g.Target + ":" + g.CoinReward + ":" + g.GemReward + ":" + g.StartValue));

        [Test] public void FeeUpgradesAndOpeningTheCarParkDoNotMoveTodaysGoals()
        {
            var store = OpenClinic();
            var before = Fingerprint(store, now);
            var sim = new ClinicSimulation(store.Profile.state);
            var fee = ClinicRules.VisitFee(sim.State);
            Assert.That(sim.Decorate(ClinicRoom.Reception).Success, Is.True);
            Assert.That(ClinicRules.VisitFee(sim.State), Is.GreaterThan(fee), "The visit fee went up.");
            Assert.That(sim.UpgradeAmenity(ClinicAmenity.Parking).Success, Is.True, "Parking makes a parking goal eligible.");
            Assert.That(store.Save(store.Profile, now.AddHours(1)), Is.True, store.Error);
            Assert.That(Fingerprint(store, now.AddHours(1)), Is.EqualTo(before));
            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now.AddHours(2));
            Assert.That(Fingerprint(reloaded, now.AddHours(2)), Is.EqualTo(before), "A relaunch keeps the same goals.");
        }

        [Test] public void ProgressNeverGoesBackwardsAndCompletedGoalsStayCompleted()
        {
            var store = OpenClinic();
            var goal = store.DailyGoals(now).First(g => g.Measure == ClinicDailyMeasure.Treatments);
            var sim = new ClinicSimulation(store.Profile.state);
            var last = 0L;
            for (var i = 0; i < 400 && !store.IsDailyDone(goal); i++)
            {
                sim.Advance(30, false);
                foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id);
                var progress = store.DailyProgress(store.DailyGoals(now).First(g => g.Id == goal.Id));
                Assert.That(progress, Is.GreaterThanOrEqualTo(last));
                last = progress;
            }
            Assert.That(store.IsDailyDone(goal), Is.True);
            Assert.That(sim.Decorate(ClinicRoom.FirstAid).Success, Is.True);
            Assert.That(store.IsDailyDone(store.DailyGoals(now).First(g => g.Id == goal.Id)), Is.True, "A fee upgrade cannot undo a finished goal.");
        }

        [Test] public void MidnightBringsFreshGoalsAndYesterdaysCannotBeCollected()
        {
            var store = OpenClinic();
            var today = store.DailyGoals(now).Select(g => g.Id).ToList();
            var midnight = new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
            Assert.That(store.Profile.daily.day, Is.EqualTo(ClinicDaily.Day(now)));
            var tomorrow = store.DailyGoals(midnight);
            Assert.That(store.Profile.daily.day, Is.EqualTo(ClinicDaily.Day(now) + 1));
            Assert.That(tomorrow.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
            Assert.That(store.Profile.daily.claimed, Is.Empty);
            Assert.That(store.Profile.daily.bonusPaid, Is.False);
            Assert.That(tomorrow.All(g => g.StartValue == store.DailyTotals.Of(g.Measure)), Is.True, "Tomorrow counts from midnight.");
            Assert.That(today, Is.Not.Empty);
        }

        [Test] public void AllThreeGoalsPayTheBonusExactlyOnceEvenAfterARelaunch()
        {
            var store = OpenClinic();
            var goals = store.DailyGoals(now).ToList();
            var sim = new ClinicSimulation(store.Profile.state);
            for (var i = 0; i < 2000 && !goals.All(store.IsDailyDone); i++)
            {
                sim.Advance(30, false);
                foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id);
                sim.CollectVendingTips(); sim.CollectParkingFees();
                var cheapest = ClinicPurchases.Options(sim).OrderBy(o => o.Cost).FirstOrDefault();
                if (cheapest != null && cheapest.Cost <= sim.State.Wallet) ClinicPurchases.Buy(sim, cheapest);
            }
            Assert.That(goals.All(store.IsDailyDone), Is.True, string.Join(",", goals.Select(g => g.Id + "=" + store.DailyProgress(g) + "/" + g.Target)));
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);

            long Bonus(int index)
            {
                var gems = store.Profile.premium.gems;
                Assert.That(store.ClaimDailyGoal(goals[index].Id, now), Is.True, store.Error);
                return store.Profile.premium.gems - gems - goals[index].GemReward;
            }
            Assert.That(Bonus(0), Is.Zero);
            Assert.That(Bonus(1), Is.Zero);
            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            store = reloaded;
            Assert.That(Bonus(2), Is.EqualTo(ClinicDaily.AllGoalsGemBonus), "The third goal completes the set.");
            Assert.That(store.Profile.daily.bonusPaid, Is.True);
            foreach (var goal in goals) Assert.That(store.ClaimDailyGoal(goal.Id, now), Is.False, "Each goal pays once a day.");
        }

        [Test] public void ASaveFromBeforeFixedGoalsKeepsTodaysClaimsAndBaselines()
        {
            var store = OpenClinic();
            var day = ClinicDaily.Day(now);
            var daily = store.Profile.daily;
            store.DailyGoals(now);
            var legacyIds = ClinicDaily.Goals(day, store.Profile.state, false).Select(g => g.Id).ToList();
            daily.claimed.Clear(); daily.claimed.Add(legacyIds[0]);
            daily.baseTreatments = Math.Max(0, store.Profile.state.TotalTreatments - 5);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            WriteWithoutFixedGoals(store.Profile);

            var legacy = new ClinicProfileStore(directory); legacy.LoadClinic(now);
            var goals = legacy.DailyGoals(now);
            Assert.That(goals.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
            Assert.That(goals.Select(g => g.Id), Does.Contain(legacyIds[0]), "The goal collected this morning is still listed.");
            Assert.That(legacy.IsDailyClaimed(legacyIds[0]), Is.True);
            Assert.That(legacy.ClaimDailyGoal(legacyIds[0], now), Is.False, "No second payment after the update.");
            var treat = goals.FirstOrDefault(g => g.Id == "daily.treat");
            if (treat != null) Assert.That(treat.StartValue, Is.EqualTo(daily.baseTreatments), "Existing goals still count from the start of the day.");
            Assert.That(legacy.Save(legacy.Profile, now), Is.True, legacy.Error);
            var again = new ClinicProfileStore(directory); again.LoadClinic(now);
            Assert.That(Fingerprint(again, now), Is.EqualTo(Fingerprint(legacy, now)));
        }

        [Test] public void AFailedClaimSpendsNothingAndPaysNothing()
        {
            var store = OpenClinic();
            var goal = store.DailyGoals(now).First(g => g.Measure == ClinicDailyMeasure.Treatments);
            var sim = new ClinicSimulation(store.Profile.state);
            for (var i = 0; i < 400 && !store.IsDailyDone(goal); i++) { sim.Advance(30, false); foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id); }
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            var gems = store.Profile.premium.gems; var wallet = store.Profile.state.Wallet;
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.ClaimDailyGoal(goal.Id, now), Is.False);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems));
            Assert.That(store.Profile.state.Wallet, Is.EqualTo(wallet));
            Assert.That(store.IsDailyClaimed(goal.Id), Is.False);
            Directory.Delete(path + ".tmp");
            Assert.That(store.ClaimDailyGoal(goal.Id, now), Is.True, store.Error);
        }

        [Test] public void InvalidStoredGoalsAreNeverWritten()
        {
            var store = OpenClinic();
            store.DailyGoals(now);
            store.Profile.daily.goals[0].target = 0;
            Assert.That(store.Save(store.Profile, now), Is.False);
            store.Profile.daily.goals[0].target = 10;
            store.Profile.daily.goals[0].measure = 99;
            Assert.That(store.Save(store.Profile, now), Is.False);
        }

        [Test] public void TheDailyDefinitionChangesAtMidnightEvenWhenEveryCountStaysTheSame()
        {
            // Review d84ac3e #1: an open Today sheet must rebuild on rollover even if badges, balances and claims are unchanged.
            var store = OpenClinic();
            var evening = new DateTimeOffset(now.UtcDateTime.Date.AddHours(23).AddMinutes(59), TimeSpan.Zero);
            var before = store.DailyDefinitionKey(evening);
            var ready = store.DailyReady(evening); var claimed = store.Profile.daily.claimed.Count; var gems = store.Profile.premium.gems;
            var after = store.DailyDefinitionKey(evening.AddMinutes(2));
            Assert.That(store.DailyReady(evening.AddMinutes(2)), Is.EqualTo(ready), "Counts alone cannot reveal the new day.");
            Assert.That(store.Profile.daily.claimed.Count, Is.EqualTo(claimed));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems));
            Assert.That(after, Is.Not.EqualTo(before));
            Assert.That(store.DailyDefinitionKey(evening.AddMinutes(3)), Is.EqualTo(after), "Stable within the day.");
        }

        [Test] public void ACollectButtonFromBeforeMidnightCannotCollectTodaysGoal()
        {
            var store = OpenClinic();
            var goal = store.DailyGoals(now).First(g => g.Measure == ClinicDailyMeasure.Treatments);
            var yesterday = ClinicDaily.Day(now);
            var tomorrow = now.AddDays(1);
            var sim = new ClinicSimulation(store.Profile.state);
            var todays = store.DailyGoals(tomorrow).First();
            for (var i = 0; i < 400 && !store.IsDailyDone(todays); i++) { sim.Advance(30, false); foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id); }
            Assert.That(store.Save(store.Profile, tomorrow), Is.True, store.Error);
            var gems = store.Profile.premium.gems;
            Assert.That(store.ClaimDailyGoal(todays.Id, yesterday, tomorrow), Is.False, "The button was drawn for yesterday's list.");
            Assert.That(store.Error, Does.Contain("new day"));
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems));
            Assert.That(store.ClaimDailyGoal(todays.Id, ClinicDaily.Day(tomorrow), tomorrow), Is.True, store.Error);
            Assert.That(goal.Id, Is.Not.Empty);
        }

        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        public void ALegacyDayKeepsItsBonusStatusAndNeverPaysItTwice(int collected, bool alreadyPaid)
        {
            // Review d84ac3e #2: before fixed goals, the bonus paid when the third goal was collected. A four-claim day was possible
            // when opening the car park swapped the goal list after the bonus had paid.
            var store = OpenClinic();
            var day = ClinicDaily.Day(now);
            store.DailyGoals(now);
            var ids = new[] { "daily.treat", "daily.collect", "daily.spend", "daily.parking" }.Take(collected).ToList();
            store.Profile.daily.claimed.Clear(); store.Profile.daily.claimed.AddRange(ids);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            WriteWithoutFixedGoals(store.Profile);

            var legacy = new ClinicProfileStore(directory); legacy.LoadClinic(now);
            var goals = legacy.DailyGoals(now);
            Assert.That(goals.Select(g => g.Id), Is.SupersetOf(ids), "Every goal collected before the update is still listed as collected.");
            Assert.That(legacy.DailyBonusPaid, Is.EqualTo(alreadyPaid));
            var open = goals.Where(g => !legacy.IsDailyClaimed(g.Id)).ToList();
            if (alreadyPaid) { Assert.That(open, Is.Empty, "All of today's goals were already collected."); return; }

            Assert.That(open.Count, Is.EqualTo(1));
            var sim = new ClinicSimulation(legacy.Profile.state);
            for (var i = 0; i < 2000 && !legacy.IsDailyDone(open[0]); i++)
            {
                sim.Advance(30, false); foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id);
                sim.CollectVendingTips(); sim.CollectParkingFees();
                var cheapest = ClinicPurchases.Options(sim).OrderBy(o => o.Cost).FirstOrDefault();
                if (cheapest != null && cheapest.Cost <= sim.State.Wallet) ClinicPurchases.Buy(sim, cheapest);
            }
            Assert.That(legacy.Save(legacy.Profile, now), Is.True, legacy.Error);
            var gems = legacy.Profile.premium.gems;
            Assert.That(legacy.ClaimDailyGoal(open[0].Id, now), Is.True, legacy.Error);
            Assert.That(legacy.Profile.premium.gems, Is.EqualTo(gems + open[0].GemReward + ClinicDaily.AllGoalsGemBonus), "The bonus was still owed.");
            Assert.That(legacy.DailyBonusPaid, Is.True);
            Assert.That(day, Is.EqualTo(ClinicDaily.Day(now)));
        }

        [Test] public void WhenUpgradesCannotBeAssessedTheDayGetsAchievableGoalsAndKeepsAnyAlreadySet()
        {
            // Review d84ac3e #4: a failed assessment must never guarantee a spending goal.
            var store = OpenClinic();
            var failing = new ClinicProfileStore(directory, (starter, doctors) => throw new InvalidOperationException("assessment failed"));
            failing.LoadClinic(now.AddDays(1));
            var goals = failing.DailyGoals(now.AddDays(1));
            Assert.That(goals.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
            Assert.That(goals.Any(g => g.Measure == ClinicDailyMeasure.Spent), Is.False, "No spending goal when spending cannot be assessed.");
            Assert.That(failing.Save(failing.Profile, now.AddDays(1)), Is.True, failing.Error);

            var planned = store.DailyGoals(now).Select(g => g.Id).ToList();
            var later = new ClinicProfileStore(directory, (starter, doctors) => throw new InvalidOperationException("assessment failed"));
            later.LoadClinic(now.AddDays(1).AddHours(1));
            Assert.That(later.DailyGoals(now.AddDays(1).AddHours(1)).Select(g => g.Id), Is.EqualTo(goals.Select(g => g.Id)), "Today's goals were already fixed.");
            Assert.That(planned, Is.Not.Empty);
        }

        /// <summary>Rewrite the save as a 4.3 (22) build would have: today's claims and baselines, but no fixed goal list.</summary>
        private void WriteWithoutFixedGoals(ClinicProfile profile)
        {
            var copy = JsonUtility.FromJson<ClinicProfile>(JsonUtility.ToJson(profile));
            copy.daily.goals.Clear(); copy.daily.bonusPaid = false;
            var payload = JsonUtility.ToJson(copy).Replace(",\"goals\":[]", "").Replace(",\"bonusPaid\":false", "");
            string digest;
            using (var hash = SHA256.Create()) digest = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            File.WriteAllText(path, JsonUtility.ToJson(new Envelope { payload = payload, checksum = digest }));
            File.Delete(path + ".backup");
        }
    }
}
