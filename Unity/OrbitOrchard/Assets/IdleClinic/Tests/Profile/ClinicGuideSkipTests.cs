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
    /// <summary>Optional gem lessons can be skipped and revisited; the finish lesson counts only work finished early;
    /// saves from before the fix keep every claim and the finish lesson they may already have seen as done.</summary>
    public sealed class ClinicGuideSkipTests
    {
        private string directory, path;
        private DateTimeOffset now;
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload, checksum; }

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-guide-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            now = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        /// <summary>A finished opening with the first two lessons collected, so the optional decor lesson is current.</summary>
        private ClinicProfileStore AtDecorLesson()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var sim = new ClinicSimulation(store.Profile.state);
            FinishOpening(sim);
            store.Profile.premium.milestonesClaimed.AddRange(new[] { "guide.equipment", "guide.waiting" });
            store.Profile.premium.gemsEarned += 10; store.Profile.premium.gems += 10;
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            Assert.That(store.CurrentGuideStep().Id, Is.EqualTo("guide.decor"));
            return store;
        }

        internal static void FinishOpening(ClinicSimulation sim)
        {
            for (var guard = 0; guard < 200 && sim.State.OpeningStep != ClinicOpeningStep.Complete; guard++)
            {
                var step = sim.State.OpeningStep;
                if (sim.State.OpeningTaskEndsTick < 0 && step != ClinicOpeningStep.FirstCheckIn && step != ClinicOpeningStep.QueueArrivals)
                    sim.ContinueOpening(step);
                sim.Advance(2, false);
                foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
            }
            Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.Complete));
        }

        [Test] public void SkippingAnOptionalLessonPaysNothingPersistsAndMovesOn()
        {
            var store = AtDecorLesson();
            var gems = store.Profile.premium.gems;
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.True, store.Error);
            Assert.That(store.CurrentGuideStep().Id, Is.EqualTo("guide.renovate"), "The free lessons carry on.");
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems), "Skipping earns and spends nothing.");
            Assert.That(store.Profile.premium.milestonesClaimed, Does.Not.Contain("guide.decor"));
            Assert.That(store.SkippedGuideSteps().Select(s => s.Id), Is.EqualTo(new[] { "guide.decor" }));

            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            Assert.That(reloaded.CurrentGuideStep().Id, Is.EqualTo("guide.renovate"), "The choice survives a relaunch.");
            Assert.That(reloaded.SkippedGuideSteps().Select(s => s.Id), Is.EqualTo(new[] { "guide.decor" }));
        }

        [Test] public void FreeLessonsCannotBeSkippedAndUnknownOrCollectedOnesAreRefused()
        {
            var store = AtDecorLesson();
            Assert.That(store.SkipGuideStep("guide.renovate", now), Is.False);
            Assert.That(store.SkipGuideStep("guide.equipment", now), Is.False, "Already collected.");
            Assert.That(store.SkipGuideStep("guide.nope", now), Is.False);
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.True, store.Error);
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.False, "Already skipped.");
            Assert.That(store.Profile.guide.skipped, Is.EqualTo(new[] { "guide.decor" }));
        }

        [Test] public void ASkippedLessonCanBeResumedAndCollectedOnceActuallyDone()
        {
            var store = AtDecorLesson();
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.True, store.Error);
            Assert.That(store.ResumeGuideStep("guide.decor", now), Is.True, store.Error);
            Assert.That(store.CurrentGuideStep().Id, Is.EqualTo("guide.decor"));
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.True, store.Error);

            // Decorating later, with gems the player chose to spend, still earns the lesson's reward once.
            Assert.That(store.BuyDecoration(ClinicRoom.Reception, now), Is.True, store.Error);
            var decor = ClinicGuide.Find("guide.decor");
            Assert.That(store.IsGuideStepDone(decor), Is.True);
            var gems = store.Profile.premium.gems;
            Assert.That(store.ClaimGuideStep("guide.decor", now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems + decor.GemReward));
            Assert.That(store.ClaimGuideStep("guide.decor", now), Is.False);
            Assert.That(store.SkippedGuideSteps(), Is.Empty, "A collected lesson is no longer listed as skipped.");
        }

        [Test] public void DecorSpendingNoLongerCompletesTheFinishLessonButAFreeFinishDoes()
        {
            var store = AtDecorLesson();
            Assert.That(store.BuyDecoration(ClinicRoom.Reception, now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gemsSpent, Is.GreaterThan(0));
            var finish = ClinicGuide.Find("guide.finish");
            Assert.That(store.IsGuideStepDone(finish), Is.False, "Decor is not finishing a renovation.");

            var sim = new ClinicSimulation(store.Profile.state);
            ClinicSimulation.TryGrantReward(sim.State, 100000);
            // The office sets the clinic level: raise it before renovating any room past level 1.
            Assert.That(sim.UpgradeOffice().Success, Is.True);
            sim.Advance(sim.State.OfficeUpgradeEndsTick / 10d, false);
            Assert.That(sim.State.OfficeLevel, Is.EqualTo(2));
            Assert.That(sim.Renovate(ClinicRoom.Reception).Success, Is.True);
            var job = sim.State.Construction.Single();
            sim.Advance(Math.Max(0, (job.EndsTick - sim.State.Tick) / 10d - 30), false);
            Assert.That(ClinicPremiumRules.SkipCost(sim.State, job), Is.Zero, "The last minute is free.");
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            var spent = store.Profile.premium.gemsSpent;
            Assert.That(store.SkipConstruction(job.Id, now), Is.True, store.Error);
            Assert.That(store.Profile.premium.gemsSpent, Is.EqualTo(spent), "A free finish spends nothing.");
            Assert.That(store.Profile.guide.workFinishedEarly, Is.EqualTo(1));
            Assert.That(store.IsGuideStepDone(finish), Is.True);
        }

        [Test] public void APreFixSaveKeepsClaimsAndItsAlreadySatisfiedFinishLessonExactlyOnce()
        {
            var store = AtDecorLesson();
            Assert.That(store.BuyDecoration(ClinicRoom.Reception, now), Is.True, store.Error);
            WriteWithoutGuideState(store.Profile);

            var legacy = new ClinicProfileStore(directory); legacy.LoadClinic(now);
            Assert.That(legacy.Profile.premium.milestonesClaimed, Is.EquivalentTo(new[] { "guide.equipment", "guide.waiting" }), "Earned claims are kept.");
            Assert.That(legacy.Profile.guide.legacyGemSpend, Is.True);
            var finish = ClinicGuide.Find("guide.finish");
            Assert.That(legacy.IsGuideStepDone(finish), Is.True, "Shown as done before the update, so still collectable.");
            Assert.That(legacy.ClaimGuideStep("guide.finish", now), Is.True, legacy.Error);
            Assert.That(legacy.ClaimGuideStep("guide.finish", now), Is.False, "Once.");

            var again = new ClinicProfileStore(directory); again.LoadClinic(now);
            Assert.That(again.Profile.guide.version, Is.EqualTo(ClinicGuideState.CurrentVersion));
            Assert.That(again.Profile.premium.milestonesClaimed.Count(id => id == "guide.finish"), Is.EqualTo(1));
        }

        [Test] public void APreFixSaveThatNeverSpentGemsGetsNoFinishCredit()
        {
            var store = AtDecorLesson();
            WriteWithoutGuideState(store.Profile);
            var legacy = new ClinicProfileStore(directory); legacy.LoadClinic(now);
            Assert.That(legacy.Profile.guide.legacyGemSpend, Is.False);
            Assert.That(legacy.IsGuideStepDone(ClinicGuide.Find("guide.finish")), Is.False);
            Assert.That(legacy.CurrentGuideStep().Id, Is.EqualTo("guide.decor"));
        }

        [Test] public void AFailedSaveLeavesTheLessonCurrentAndNothingSkipped()
        {
            var store = AtDecorLesson();
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.False);
            Assert.That(store.CurrentGuideStep().Id, Is.EqualTo("guide.decor"));
            Assert.That(store.Profile.guide.skipped, Is.Empty);
            Directory.Delete(path + ".tmp");
            Assert.That(store.SkipGuideStep("guide.decor", now), Is.True, store.Error);
        }

        [Test] public void TamperedSkipListsAreRejectedRatherThanTrusted()
        {
            var store = AtDecorLesson();
            store.Profile.guide.skipped.Add("");
            Assert.That(store.Save(store.Profile, now), Is.False, "Invalid guide state is never written.");
            store.Profile.guide.skipped.Clear();
            store.Profile.guide.workFinishedEarly = -1;
            Assert.That(store.Save(store.Profile, now), Is.False);
        }

        /// <summary>Rewrite the save as a pre-4.3 (23) build would have: identical, but with no guide state at all.</summary>
        private void WriteWithoutGuideState(ClinicProfile profile)
        {
            var json = JsonUtility.ToJson(profile);
            var start = json.IndexOf("\"guide\":", StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThan(0));
            var depth = 0; var end = json.IndexOf('{', start);
            for (; end < json.Length; end++) { if (json[end] == '{') depth++; else if (json[end] == '}' && --depth == 0) break; }
            var cut = json[start - 1] == ',' ? start - 1 : start;
            var payload = json.Remove(cut, end + 1 - cut);
            Assert.That(payload, Does.Not.Contain("\"guide\""));
            string digest;
            using (var hash = SHA256.Create()) digest = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            File.WriteAllText(path, JsonUtility.ToJson(new Envelope { payload = payload, checksum = digest }));
            File.Delete(path + ".backup");
        }
    }
}
