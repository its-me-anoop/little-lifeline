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
    /// <summary>The opening story on screen: Maya's lines in the guide card, naming the boss, answers that start their task,
    /// skipping, hiring Maya, her name at her station, the first lesson, and the story so far in Settings. Older saves keep
    /// the plain opening.</summary>
    public sealed class ClinicStoryHUDTests
    {
        private const string Hello = "“Hello? Oh! You must be the new owner. I’m Maya.”";

        [Test]
        public void ANewGameOpensWithMayaAndTheCardWalksThroughThePrologue()
        {
            using (var ui = new StoryApp())
            {
                Assert.That(ui.Title, Is.EqualTo(Hello));
                Assert.That(ui.Eyebrow, Is.EqualTo("MAYA"));
                Assert.That(ui.Why, Is.EqualTo("Tap to continue"));
                var portrait = ui.Get<VisualElement>("storyPortrait");
                Assert.That(portrait.style.display.value, Is.EqualTo(DisplayStyle.Flex), "Maya's portrait in the card while she talks.");
                Assert.That(ui.PortraitName(portrait), Is.EqualTo("Maya"));
                Assert.That(ui.Get<ClinicIcon>("guideGlyph").parent.style.display.value, Is.EqualTo(DisplayStyle.None), "The flag tile makes way.");
                Assert.That(ui.Get<Button>("storySkip").style.display.value, Is.EqualTo(DisplayStyle.Flex), "The story can be skipped from the first line.");
                ui.Invoke("OnGuideTap");
                Assert.That(ui.Title, Does.Contain("high street surgery"));
                ui.Invoke("OnGuideTap"); ui.Invoke("OnGuideTap");
                Assert.That(ui.Title, Does.Contain("catch your name"));
                Assert.That(ui.Why, Is.EqualTo("Tap to answer"));
                Assert.That(ui.Profile.story.line, Is.EqualTo(3));
                Assert.That(ui.Simulation.State.OpeningTaskEndsTick, Is.EqualTo(-1), "Reading starts nothing.");

                ui.Invoke("OnGuideTap");
                var dock = ui.Dock;
                Assert.That(dock.style.display.value, Is.EqualTo(DisplayStyle.Flex), "The question opens the panel.");
                Assert.That(dock.Q<Label>(className: "dock-title").text, Is.EqualTo("Meet Maya"));
                Assert.That(dock.Q(className: "story-quote"), Is.Not.Null);
                Assert.That(ui.PortraitName(dock.Q("story-quote-portrait")), Is.EqualTo("Maya"), "Her portrait beside her question.");
                Assert.That(ui.PortraitName(dock.Q("story-you")), Is.EqualTo("Boss"), "The boss's portrait above the player's answers.");
                Assert.That(dock.Q("story-you").Q<Label>().text, Is.EqualTo("YOU"));
                var field = dock.Q<TextField>("story-name");
                Assert.That(field, Is.Not.Null);
                Assert.That(dock.Q<Button>("story-reply-0").enabledSelf, Is.False, "A name needs a letter first.");
                Assert.That(dock.Q<Button>("story-reply-1"), Is.Not.Null);
                Assert.That(dock.Q<Button>("opening-action"), Is.Null, "No task until the prologue is answered.");

                field.value = "  Sam  ";
                ui.Activate(dock.Q<Button>("story-reply-0"));
                Assert.That(ui.Profile.story.bossName, Is.EqualTo("Sam"));
                dock = ui.Dock;
                Assert.That(dock.Q(className: "story-quote").Q<Label>(className: "story-line").text, Is.EqualTo("“Nice to meet you, Sam! Where do we start?”"));
                Assert.That(dock.Q("story-you").Q<Label>().text, Is.EqualTo("YOU · SAM"), "Named once the boss has a name.");
                var start = dock.Q<Button>("story-reply-1");
                Assert.That(start.Q<Label>(className: "story-reply-label").text, Is.EqualTo("Biscuits first. Then my office."));

                ui.Activate(start);
                Assert.That(ui.Simulation.State.OpeningTaskEndsTick, Is.GreaterThanOrEqualTo(0), "The answer starts cleaning the office.");
                Assert.That(ui.Simulation.State.BossFromZone, Is.EqualTo(ClinicBossRoute.Entrance), "Walking there together from the door.");
                Assert.That(ui.Reactions, Does.Contain("I\u2019m Sam.").And.Contain("Biscuits first. Then my office."), "The boss answers aloud.");
                Assert.That(ui.Profile.story.Choice("prologue.start"), Is.EqualTo(1));
                Assert.That(ui.Dock.style.display.value, Is.EqualTo(DisplayStyle.None), "The panel closes so the walk can be watched.");
                Assert.That(ui.Title, Is.EqualTo("“Is that a box of… more boxes?”"));
                Assert.That(ui.Eyebrow, Is.EqualTo("MAYA · 1 OF 12"));
                Assert.That(ui.Why, Does.StartWith("You’re cleaning"));
                var saved = new ClinicProfileStore(ui.Directory).LoadClinic(DateTimeOffset.UtcNow);
                Assert.That(saved.story.bossName, Is.EqualTo("Sam"), "The name is saved on this device.");
                Assert.That(saved.story.line, Is.EqualTo(5));
            }
        }

        [Test]
        public void JustCallMeBossAndPitchingInDrawRemarks()
        {
            using (var ui = new StoryApp())
            {
                for (var i = 0; i < 4; i++) ui.Invoke("OnGuideTap");
                ui.Activate(ui.Dock.Q<Button>("story-reply-1"));
                Assert.That(ui.Profile.story.bossName, Is.Empty);
                Assert.That(ui.Dock.Q(className: "story-quote").Q<Label>(className: "story-line").text, Does.Contain("Nice to meet you, boss!"));
                ui.Activate(ui.Dock.Q<Button>("story-reply-0"));
                ui.Simulation.Advance(5);
                ui.Invoke("OnGuideTap");
                var help = ui.Dock.Q<Button>("opening-help");
                Assert.That(help.Q<Label>(className: "action-label").text, Is.EqualTo("Pitch in"));
                ui.Activate(help);
                Assert.That(ui.Reactions, Does.Contain("Look at you, sleeves rolled up!"));
                var once = ui.Reactions.Count;
                ui.Activate(ui.Dock.Q<Button>("opening-help"));
                Assert.That(ui.Reactions.Count(r => r.StartsWith("Look at you", StringComparison.Ordinal)), Is.EqualTo(1), "Said once.");
                Assert.That(ui.Reactions.Count, Is.EqualTo(once));
            }
        }

        [Test]
        public void SkippingKeepsThePlainOpeningAndMayaAsTheFirstNurse()
        {
            using (var ui = new StoryApp())
            {
                ui.Activate(ui.Get<Button>("storySkip"));
                Assert.That(ui.Profile.story.skipped, Is.True);
                Assert.That(ui.Title, Is.EqualTo("Clean your office"));
                Assert.That(ui.Eyebrow, Is.EqualTo("OPEN YOUR CLINIC · 1 OF 12"));
                Assert.That(ui.Get<Button>("storySkip").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(ui.Get<VisualElement>("storyPortrait").style.display.value, Is.EqualTo(DisplayStyle.None), "No portrait once the story is skipped.");
                Assert.That(ui.Get<ClinicIcon>("guideGlyph").parent.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                var cast = (ClinicStoryCast)ui.Invoke("StoryCast");
                Assert.That(cast.Enabled, Is.True, "Maya is still the first nurse.");
                Assert.That(cast.Greeting, Is.False, "The boss leaves the door; she goes along.");
                Assert.That(cast.KeepTogether, Is.False);
                ui.Invoke("OnGuideTap");
                Assert.That(ui.Dock.Q<Button>("opening-action"), Is.Not.Null);
                Assert.That(ui.Dock.Q(className: "story-quote"), Is.Null);
                Assert.That(ui.Profile.story.bossName, Is.Empty, "Skipping keeps the player as the boss.");
            }
        }

        [Test]
        public void HiringMayaIsAnAnswerAndHerStationKeepsHerName()
        {
            using (var ui = new StoryApp())
            {
                ui.FinishPrologue();
                ui.PlayOpeningTo(ClinicOpeningStep.HireNurse);
                Assert.That(ui.Title, Is.EqualTo("“So… about that job?”"));
                ui.Invoke("OnGuideTap");
                var hire = ui.Dock.Q<Button>("opening-action");
                Assert.That(hire.Q<Label>(className: "story-reply-label").text, Is.EqualTo("You’re hired, Maya."));
                Assert.That(ui.PortraitName(ui.Dock.Q("story-you")), Is.EqualTo("Boss"), "The job offer is the boss's to make.");
                Assert.That(ui.Dock.Q<Button>("story-reply-1").Q<Label>(className: "story-reply-label").text, Is.EqualTo("Welcome aboard, Maya."));
                ui.Activate(hire);
                Assert.That(ui.Simulation.State.Staff.Any(s => s.Id == ClinicStory.FirstNurseStaffId), Is.True);
                Assert.That(ui.Reactions.IndexOf("You\u2019re hired, Maya."), Is.LessThan(ui.Reactions.IndexOf("Thanks, boss. Let\u2019s get to work.")), "The boss speaks, then Maya answers.");
                Assert.That(ui.Profile.story.Choice("step.hire-maya"), Is.Zero);
                Assert.That(ui.Reactions, Does.Contain("Thanks, boss. Let’s get to work."));

                ui.PlayOpeningTo(ClinicOpeningStep.Complete);
                Assert.That(ui.Reactions, Does.Contain("We’re open, boss. Welcome to Little Lifeline."));
                Assert.That(ui.Why, Is.EqualTo("Maya: “Better kit means quicker care. Let’s upgrade my equipment.”"), "Maya introduces the first lesson.");
                var station = ui.Open(new ClinicHit(ClinicHitKind.Station, 0));
                Assert.That(station.Query<Label>().ToList().Any(l => l.text.StartsWith("Maya · ", StringComparison.Ordinal)), Is.True);
                Assert.That(station.Q<Button>("staff-training-" + ClinicStory.FirstNurseStaffId).tooltip, Does.StartWith("Maya’s training"));
            }
        }

        [Test]
        public void TheStoryPageInSettingsShowsTheConversationAndRenamesTheBoss()
        {
            using (var ui = new StoryApp())
            {
                ui.Invoke("OnGuideTap");
                ui.Set("settingsOpen", true);
                ui.Invoke("UpdateReadouts");
                var link = ui.Dock.Q<Button>("settings-story");
                Assert.That(link, Is.Not.Null);
                ui.Activate(link);
                var dock = ui.Dock;
                Assert.That(dock.Q<Label>(className: "dock-title").text, Is.EqualTo("Story"));
                Assert.That(dock.Q("story-log").Query<Label>(className: "story-entry-text").ToList().Select(l => l.text), Is.EqualTo(new[]
                {
                    "Hello? Oh! You must be the new owner. I’m Maya.",
                    "The high street surgery closed. Half the town has a cough and nowhere to go.",
                }));
                ui.Invoke("RenameBoss", "Priya");
                Assert.That(ui.Profile.story.bossName, Is.EqualTo("Priya"));
                Assert.That(ui.Dock.Q<TextField>("story-rename").value, Is.EqualTo("Priya"));
                ui.Activate(ui.Dock.Q<Button>("story-back"));
                Assert.That(ui.Dock.Q<Label>(className: "dock-title").text, Is.EqualTo("Settings"));
            }
        }

        /// <summary>The portraits are original pictures in Resources/Portraits, small enough for a phone, shared by every place
        /// they appear, and a missing one shows the speaker's symbol instead of a hole. The speech bubbles carry them too.</summary>
        [Test]
        public void PortraitsAreSharedModestAndAlwaysHaveAFallback()
        {
            using (var ui = new StoryApp())
            {
                foreach (var speaker in new[] { "Maya", "Boss" })
                {
                    var texture = Resources.Load<Texture2D>("Portraits/" + speaker);
                    Assert.That(texture, Is.Not.Null, speaker);
                    Assert.That(Math.Max(texture.width, texture.height), Is.LessThanOrEqualTo(512), speaker + " stays small on a phone.");
                    Assert.That(texture.width, Is.EqualTo(texture.height), speaker + " is square, so it never stretches.");
                }
                Assert.That(ui.PortraitName(ui.Get<VisualElement>("storyBubble")), Is.EqualTo("Maya"));
                Assert.That(ui.PortraitName(ui.Get<VisualElement>("bossBubble")), Is.EqualTo("Boss"));
                var card = ui.Get<VisualElement>("storyPortrait").Q<Image>().image;
                Assert.That(ui.Get<VisualElement>("storyBubble").Q<Image>().image, Is.SameAs(card), "One texture, shared.");
                var host = new VisualElement();
                var missing = (VisualElement)ui.Invoke("StoryPortrait", host, "Nobody", "story-test", true);
                Assert.That(missing.Q<Image>(), Is.Null);
                Assert.That(missing.Q<ClinicIcon>(), Is.Not.Null, "A missing portrait shows a symbol.");
                Assert.That(missing.ClassListContains("story-portrait-missing"), Is.True);
                Assert.That(missing.pickingMode, Is.EqualTo(PickingMode.Ignore), "Portraits are decoration: taps go to the card.");
            }
        }

        [Test]
        public void OlderSavesKeepThePlainOpeningAndNoStorySettings()
        {
            using (var ui = new StoryApp(story: false))
            {
                Assert.That(ui.Title, Is.EqualTo("Clean your office"));
                Assert.That(ui.Eyebrow, Is.EqualTo("OPEN YOUR CLINIC · 1 OF 12"));
                Assert.That(ui.Get<Button>("storySkip").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(ui.Get<VisualElement>("storyPortrait").style.display.value, Is.EqualTo(DisplayStyle.None));
                Assert.That(((ClinicStoryCast)ui.Invoke("StoryCast")).Enabled, Is.False);
                ui.Set("settingsOpen", true);
                ui.Invoke("UpdateReadouts");
                Assert.That(ui.Dock.Q<Button>("settings-story"), Is.Null);
                ui.Set("settingsOpen", false);
                ui.PlayOpeningTo(ClinicOpeningStep.Complete);
                Assert.That(ui.Why, Is.EqualTo(ClinicGuide.Find("guide.equipment").Why));
                var station = ui.Open(new ClinicHit(ClinicHitKind.Station, 0));
                Assert.That(station.Query<Label>().ToList().Any(l => l.text.Contains("Maya")), Is.False);
            }
        }

        [Test]
        public void AMissingScriptFallsBackToThePlainOpening()
        {
            using (var ui = new StoryApp())
            {
                ui.Set("storyScript", new ClinicStoryScript().Normalized());
                ui.Invoke("UpdateReadouts");
                Assert.That(ui.Title, Is.EqualTo("Clean your office"));
                ui.Invoke("OnGuideTap");
                Assert.That(ui.Dock.Q<Button>("opening-action"), Is.Not.Null, "The opening can always be played.");
            }
        }

        /// <summary>A real profile store in a temporary folder, a real world, and just enough of the interface: the guide card,
        /// the story controls and the panel.</summary>
        private sealed class StoryApp : IDisposable
        {
            private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
            public readonly string Directory = Path.Combine(Path.GetTempPath(), "clinic-story-ui-" + Guid.NewGuid().ToString("N"));
            private readonly GameObject owner, worldHost;
            private readonly ClinicApp app;
            private readonly VisualElement root = new VisualElement();
            public readonly ClinicProfile Profile;

            public StoryApp(bool story = true)
            {
                var store = new ClinicProfileStore(Directory);
                Profile = store.LoadClinic(DateTimeOffset.UtcNow);
                if (!story) Profile.story = new ClinicStoryState();
                worldHost = new GameObject("Story HUD world");
                var world = worldHost.AddComponent<ClinicWorld>(); world.Initialize();
                owner = new GameObject("Story HUD test"); owner.SetActive(false);
                app = owner.AddComponent<ClinicApp>();
                Set("saves", store); Set("profile", Profile); Set("world", world); Set("root", root); Set("overlay", root);
                Set("clinicAudio", owner.AddComponent<ClinicAudio>());
                var dock = new VisualElement(); dock.style.display = DisplayStyle.None; root.Add(dock); Set("dock", dock);
                Set("walletLabel", new Label()); Set("hint", new VisualElement()); Set("hintLabel", new Label());
                Invoke("BindSimulations"); Invoke("BuildGuide"); Invoke("BuildStory"); Invoke("UpdateReadouts");
            }

            public ClinicSimulation Simulation => Get<ClinicSimulation>("simulation");
            public VisualElement Dock => Get<VisualElement>("dock");
            public string Title => Get<Label>("guideTitle").text;
            public string Eyebrow => Get<Label>("guideEyebrow").text;
            public string Why => Get<Label>("guideWhy").text;
            public List<string> Reactions => (List<string>)Invoke("QueuedStoryRemarks");
            /// <summary>The name of the picture inside a portrait frame (or anything holding one).</summary>
            public string PortraitName(VisualElement frame) { Assert.That(frame, Is.Not.Null); return frame.Q<Image>()?.image?.name; }

            public T Get<T>(string name) => (T)typeof(ClinicApp).GetField(name, Private).GetValue(app);
            public void Set(string name, object value) => typeof(ClinicApp).GetField(name, Private).SetValue(app, value);
            public object Invoke(string name, params object[] arguments) => typeof(ClinicApp).GetMethod(name, Private).Invoke(app, arguments);

            /// <summary>Run a control's action as a tap would (the same action VoiceOver activates).</summary>
            public void Activate(VisualElement element)
            {
                Assert.That(element, Is.Not.Null);
                foreach (var binding in Get<System.Collections.IList>("accessible").Cast<object>().Reverse())
                    if ((VisualElement)binding.GetType().GetField("Element").GetValue(binding) == element)
                    { ((Action)binding.GetType().GetField("Action").GetValue(binding))(); return; }
                Assert.Fail("No action registered for " + element.name);
            }

            public VisualElement Open(ClinicHit hit)
            {
                Set("selectedRoom", null); Set("selectedObject", (ClinicHit?)hit); Set("dockKey", "");
                Invoke("UpdateReadouts");
                return Dock;
            }

            /// <summary>Answer the prologue with the first answers as taps would; the boss keeps the default name.</summary>
            public void FinishPrologue()
            {
                var script = ClinicStoryScripts.Current;
                for (var guard = 0; guard < 16; guard++)
                {
                    var beat = ClinicStory.Current(script, Profile.story, Simulation.State);
                    if (beat == null || beat.Kind != ClinicStoryBeatKind.Prologue) break;
                    if (beat.AsksName) ClinicStory.Answer(script, Profile.story, Simulation.State, 1);
                    else if (beat.Replies.Count > 0) ClinicStory.Answer(script, Profile.story, Simulation.State, 0);
                    else ClinicStory.Continue(script, Profile.story, Simulation.State);
                }
                Invoke("UpdateReadouts");
            }

            /// <summary>Play the opening to <paramref name="target"/> as taps would, looking in between as the game does.</summary>
            public void PlayOpeningTo(ClinicOpeningStep target)
            {
                var sim = Simulation;
                for (var guard = 0; guard < 4000 && sim.State.OpeningStep < target; guard++)
                {
                    var step = sim.State.OpeningStep;
                    if (sim.State.OpeningTaskEndsTick < 0 && (ClinicSimulation.OpeningManual(step) || step == ClinicOpeningStep.HireNurse || step == ClinicOpeningStep.HireReceptionist))
                        sim.ContinueOpening(step);
                    sim.Advance(.5);
                    foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
                    Invoke("UpdateReadouts");
                }
                sim.DrainEvents();
                Assert.That(sim.State.OpeningStep, Is.EqualTo(target));
                Set("dockKey", ""); Invoke("UpdateReadouts");
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(worldHost);
                if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, true);
            }
        }
    }
}
