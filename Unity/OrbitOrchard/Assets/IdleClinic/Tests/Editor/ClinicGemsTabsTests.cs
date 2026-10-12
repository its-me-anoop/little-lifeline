using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using IdleClinic.App;
using IdleClinic.Core;
using IdleClinic.Presentation;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.Tests
{
    /// <summary>Today and Goals, drawn in the Store's style: a hero card that says what is ready and what comes next, one
    /// collect control per reward, clear progress on every daily goal, and the done, ready and still-to-do states told
    /// apart. The rewards themselves come from the profile store and are unchanged.</summary>
    public sealed class ClinicGemsTabsTests
    {
        [Test]
        public void TodayLeadsWithTheDailyRewardThenTheGoalsWithTheirProgress()
        {
            using (var ui = new GemsApp())
            {
                var tab = ui.OpenTab("Today");
                var hero = tab.Q("today-hero");
                Assert.That(hero, Is.Not.Null, "The daily reward leads, like the Store's builder card.");
                Assert.That(ui.Headline(hero), Is.EqualTo("Daily reward is ready."));
                Assert.That(hero.Q<Label>("today-streak").text, Is.EqualTo("Day 1 of 7"));
                Assert.That(hero.Query(className: "today-chip").ToList().Count, Is.EqualTo(7));
                Assert.That(hero.Query(className: "today-chip-next").ToList().Count, Is.EqualTo(1), "Today's day stands out.");
                Assert.That(tab.Query<Button>("today-collect").ToList().Count, Is.EqualTo(1), "One collect control for the daily reward.");

                var goals = tab.Query(className: "daily-goal").ToList();
                Assert.That(goals.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
                foreach (var goal in goals)
                {
                    Assert.That(goal.Q<Label>(className: "daily-goal-status").text, Does.Contain(" to go"), "Every goal says how far there is to go.");
                    Assert.That(goal.Q(className: "meter-fill"), Is.Not.Null, "And shows it.");
                    Assert.That(goal.Q<Button>(), Is.Null, "Nothing to collect before it is done.");
                    Assert.That(goal.Q(className: "reward-stack"), Is.Not.Null, "The reward is shown up front.");
                }
                var bonus = tab.Q("daily-bonus");
                Assert.That(bonus, Is.Not.Null);
                Assert.That(bonus.Query(className: "bonus-segment").ToList().Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
                Assert.That(bonus.Query(className: "bonus-segment-done").ToList().Count, Is.Zero);
                Assert.That(bonus.Q<Label>(className: "bonus-chip-value").text, Is.EqualTo("+" + ClinicDaily.AllGoalsGemBonus));

                var gems = ui.Profile.premium.gems;
                ui.Activate(tab.Q<Button>("today-collect"));
                Assert.That(ui.Store.Profile.premium.gems, Is.EqualTo(gems + ClinicDaily.StreakReward(1)), "The same reward as before.");
                tab = ui.OpenTab("Today");
                hero = tab.Q("today-hero");
                Assert.That(ui.Headline(hero), Does.StartWith("Collected."));
                Assert.That(tab.Q<Button>("today-collect"), Is.Null, "Collected once; no second button.");
                Assert.That(hero.Q<Label>("today-collected").text, Does.StartWith("Collected today"));
                Assert.That(hero.Query(className: "today-chip-done").ToList().Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void ADoneDailyGoalIsReadyThenCollectedAndCountsTowardsTheBonus()
        {
            using (var ui = new GemsApp())
            {
                ui.PlayOpeningToComplete();
                var now = DateTimeOffset.UtcNow;
                ui.Store.EnsureDay(now);
                for (var guard = 0; guard < 8000 && !ui.Store.DailyGoals(now).Any(ui.Store.IsDailyDone); guard++) ui.Advance(.5);
                var done = ui.Store.DailyGoals(now).First(ui.Store.IsDailyDone);
                var tab = ui.OpenTab("Today");
                var card = tab.Q("daily-goal-" + done.Id);
                Assert.That(card.ClassListContains("daily-goal-ready"), Is.True, "A finished goal stands out.");
                Assert.That(card.Q<Label>(className: "daily-goal-status").text, Does.StartWith("Done"));
                var collect = card.Q<Button>("daily-collect-" + done.Id);
                Assert.That(collect, Is.Not.Null);
                Assert.That(tab.Query<Button>("daily-collect-" + done.Id).ToList().Count, Is.EqualTo(1));
                var gems = ui.Store.Profile.premium.gems;
                ui.Activate(collect);
                Assert.That(ui.Store.Profile.premium.gems, Is.GreaterThanOrEqualTo(gems + done.GemReward));
                tab = ui.OpenTab("Today");
                card = tab.Q("daily-goal-" + done.Id);
                Assert.That(card.ClassListContains("daily-goal-claimed"), Is.True);
                Assert.That(card.Q<Button>(), Is.Null, "Collected: nothing left to tap.");
                Assert.That(card.Q(className: "daily-goal-check"), Is.Not.Null);
                Assert.That(card.Q<Label>(className: "daily-goal-status").text, Does.StartWith("Collected"));
                Assert.That(tab.Q("daily-bonus").Query(className: "bonus-segment-done").ToList().Count, Is.GreaterThanOrEqualTo(1));
            }
        }

        [Test]
        public void GoalsSaysWhatIsReadyAndWhatComesNext()
        {
            using (var ui = new GemsApp())
            {
                var tab = ui.OpenTab("Goals");
                var hero = tab.Q("goals-hero");
                Assert.That(hero, Is.Not.Null);
                Assert.That(ui.Headline(hero), Is.EqualTo("Keep going. More gems ahead."));
                Assert.That(hero.Q<Label>("goals-tag").text, Is.EqualTo("0 of " + ClinicMilestones.All.Count + " complete"));
                var next = tab.Q("goal-next");
                Assert.That(next, Is.Not.Null, "The next goal is singled out.");
                Assert.That(next.Q<Label>(className: "gem-goal-title").text, Is.EqualTo(ClinicMilestones.All[0].Title));
                Assert.That(tab.Query(className: "goal-claim").ToList(), Is.Empty);

                ui.PlayOpeningToComplete();
                tab = ui.OpenTab("Goals");
                var ready = ui.Store.ClaimableMilestones().ToList();
                Assert.That(ready, Is.Not.Empty, "The opening treats the first patient.");
                Assert.That(ui.Headline(tab.Q("goals-hero")), Does.StartWith(ready.Count + (ready.Count == 1 ? " reward" : " rewards")));
                var first = ready[0];
                var card = tab.Q("goal-ready-" + first.Id);
                Assert.That(card, Is.Not.Null);
                Assert.That(tab.Query<Button>("goal-collect-" + first.Id).ToList().Count, Is.EqualTo(1), "One collect control per reward.");
                var gems = ui.Store.Profile.premium.gems;
                ui.Activate(card.Q<Button>("goal-collect-" + first.Id));
                Assert.That(ui.Store.Profile.premium.gems, Is.EqualTo(gems + first.GemReward));
                tab = ui.OpenTab("Goals");
                Assert.That(tab.Q("goal-ready-" + first.Id), Is.Null);
                Assert.That(tab.Q<Label>("goals-tag").text, Is.EqualTo("1 of " + ClinicMilestones.All.Count + " complete"));
            }
        }

        /// <summary>The user's marked badge: the gem confirmation for the second builder shows a builder, not an upward arrow.
        /// Other confirmations keep their symbols, the Store's large builder picture is unchanged, and cancelling spends nothing.</summary>
        [Test]
        public void TheSecondBuilderConfirmationShowsTheBuilder()
        {
            using (var ui = new GemsApp())
            {
                ui.BuildGemConfirm();
                var gems = ui.Store.Profile.premium.gems;
                ui.RequestGemSpend(ClinicGemSpendKind.ExtraBuilder, ClinicUnlocks.ExtraBuilder);
                var badge = ui.ConfirmCard.Q("gem-confirm-badge");
                Assert.That(badge, Is.Not.Null);
                Assert.That(badge.Q<Image>()?.image?.name, Is.EqualTo("BuilderBadge"));
                Assert.That(badge.Q<ClinicIcon>(), Is.Null, "No arrow beside Second builder.");
                Assert.That(badge.ClassListContains("gem-confirm-builder"), Is.True);
                ui.Activate(ui.ConfirmCard.Q<Button>("gem-confirm-close"));
                Assert.That(ui.Store.Profile.premium.gems, Is.EqualTo(gems), "Cancelling spends nothing.");

                ui.RequestGemSpend(ClinicGemSpendKind.CoinPack, ClinicShopOffers.CoinPacks[0].Id);
                badge = ui.ConfirmCard.Q("gem-confirm-badge");
                Assert.That(badge.Q<Image>(), Is.Null);
                Assert.That(badge.Q<ClinicIcon>(), Is.Not.Null, "Other spends keep their symbol.");
                ui.Activate(ui.ConfirmCard.Q<Button>("gem-confirm-close"));

                var shop = ui.OpenTab("Shop");
                Assert.That(shop.Q("shop-builder-hero").Q<Image>().image.name, Is.EqualTo("BuilderHero"), "The Store's builder picture stays.");
            }
        }

        /// <summary>A real profile store in a temporary folder, a real world, and the gems panel.</summary>
        private sealed class GemsApp : IDisposable
        {
            private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            private readonly string directory = Path.Combine(Path.GetTempPath(), "clinic-gems-ui-" + Guid.NewGuid().ToString("N"));
            private readonly GameObject owner, worldHost;
            private readonly ClinicApp app;
            private readonly VisualElement root = new VisualElement();
            public readonly ClinicProfileStore Store;
            public readonly ClinicProfile Profile;

            public GemsApp()
            {
                Store = new ClinicProfileStore(directory);
                Profile = Store.LoadClinic(DateTimeOffset.UtcNow);
                Profile.story = new ClinicStoryState();
                worldHost = new GameObject("Gems UI world");
                var world = worldHost.AddComponent<ClinicWorld>(); world.Initialize();
                owner = new GameObject("Gems UI test"); owner.SetActive(false);
                app = owner.AddComponent<ClinicApp>();
                Set("saves", Store); Set("profile", Profile); Set("world", world); Set("root", root); Set("overlay", root);
                Set("clinicAudio", owner.AddComponent<ClinicAudio>());
                var dock = new VisualElement(); dock.style.display = DisplayStyle.None; root.Add(dock); Set("dock", dock);
                Set("walletLabel", new Label()); Set("hint", new VisualElement()); Set("hintLabel", new Label());
                Invoke("BindSimulations"); Invoke("BuildGuide"); Invoke("BuildStory"); Invoke("UpdateReadouts");
            }

            private ClinicSimulation Simulation => Get<ClinicSimulation>("simulation");
            public VisualElement ConfirmCard => Get<VisualElement>("gemConfirmCard");
            public void BuildGemConfirm() => Invoke("BuildGemConfirm");
            public void RequestGemSpend(ClinicGemSpendKind kind, string key) => Invoke("RequestGemSpend", kind, key);
            private T Get<T>(string name) => (T)typeof(ClinicApp).GetField(name, Private).GetValue(app);
            private void Set(string name, object value) => typeof(ClinicApp).GetField(name, Private).SetValue(app, value);
            private object Invoke(string name, params object[] arguments) => typeof(ClinicApp).GetMethod(name, Private).Invoke(app, arguments);

            /// <summary>Open the gems panel on a tab, rebuilt from the current save.</summary>
            public VisualElement OpenTab(string tab)
            {
                var field = typeof(ClinicApp).GetField("gemTab", Private);
                field.SetValue(app, Enum.Parse(field.FieldType, tab));
                Set("gemsOpen", true); Set("dockKey", "");
                Invoke("UpdateReadouts");
                var content = Get<VisualElement>("dock").Q("clinic-gems-content");
                Assert.That(content, Is.Not.Null, tab);
                return content;
            }

            /// <summary>The hero's two headline lines read as one sentence.</summary>
            public string Headline(VisualElement hero) => string.Join(" ", hero.Query<Label>(className: "gem-hero-headline").ToList().Select(l => l.text));

            public void Activate(VisualElement element)
            {
                Assert.That(element, Is.Not.Null);
                foreach (var binding in Get<System.Collections.IList>("accessible").Cast<object>().Reverse())
                    if ((VisualElement)binding.GetType().GetField("Element").GetValue(binding) == element)
                    { ((Action)binding.GetType().GetField("Action").GetValue(binding))(); return; }
                Assert.Fail("No action registered for " + element.name);
            }

            public void Advance(double seconds)
            {
                Simulation.Advance(seconds);
                foreach (var desk in Simulation.State.ReceptionDesks) if (desk.Till > 0) Simulation.Collect(desk.Id);
            }

            public void PlayOpeningToComplete()
            {
                var sim = Simulation;
                for (var guard = 0; guard < 4000 && sim.State.OpeningStep < ClinicOpeningStep.Complete; guard++)
                {
                    var step = sim.State.OpeningStep;
                    if (sim.State.OpeningTaskEndsTick < 0 && (ClinicSimulation.OpeningManual(step) || step == ClinicOpeningStep.HireNurse || step == ClinicOpeningStep.HireReceptionist))
                        sim.ContinueOpening(step);
                    Advance(.5);
                }
                sim.DrainEvents();
                Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.Complete));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(worldHost);
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
