using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>The opening story in the save: new games tell it, older saves never gain it part way through, its place,
    /// name and answers survive a relaunch, damaged story data is repaired rather than costing the clinic, and the shipped
    /// script covers the whole opening.</summary>
    public sealed class ClinicStoryProfileTests
    {
        private string directory, path;
        private DateTimeOffset now;
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload, checksum; }

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-story-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            now = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        private static ClinicStoryScript Shipped()
            => ClinicStoryScripts.Parse(File.ReadAllText(Path.Combine(Application.dataPath, "IdleClinic/Resources/ClinicStory.json")));

        private void WritePayload(string payload)
        {
            string digest;
            using (var hash = SHA256.Create()) digest = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            File.WriteAllText(path, JsonUtility.ToJson(new Envelope { payload = payload, checksum = digest }));
            if (File.Exists(path + ".backup")) File.Delete(path + ".backup");
        }

        /// <summary>The saved payload with the story replaced (null removes it, as in a save from before the story).</summary>
        private string PayloadWithStory(ClinicProfile profile, string story)
        {
            var json = JsonUtility.ToJson(profile);
            var at = json.IndexOf(",\"story\":", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThan(0));
            Assert.That(json.EndsWith("}}", StringComparison.Ordinal), Is.True, "The story is the last field of the profile.");
            return json.Substring(0, at) + (story == null ? "" : ",\"story\":" + story) + "}";
        }

        [Test] public void NewGamesTellTheStoryAndOlderSavesKeepThePlainOpening()
        {
            var store = new ClinicProfileStore(directory);
            var profile = store.LoadClinic(now);
            Assert.That(profile.story.enabled, Is.True);
            Assert.That(profile.story.line, Is.Zero);
            Assert.That(profile.story.bossName, Is.Empty);
            Assert.That(ClinicStory.Current(Shipped(), profile.story, profile.state).Line.id, Is.EqualTo("prologue.hello"));

            var step = profile.state.OpeningStep; var wallet = profile.state.Wallet;
            WritePayload(PayloadWithStory(profile, null));
            var older = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(older.story, Is.Not.Null);
            Assert.That(older.story.enabled, Is.False, "A save from before the story never starts it part way through.");
            Assert.That(ClinicStory.Current(Shipped(), older.story, older.state), Is.Null);
            Assert.That(older.state.OpeningStep, Is.EqualTo(step));
            Assert.That(older.state.Wallet, Is.EqualTo(wallet));
        }

        [Test] public void TheStoryPositionNameAndAnswersSurviveARelaunch()
        {
            var script = Shipped();
            var store = new ClinicProfileStore(directory);
            var profile = store.LoadClinic(now);
            for (var i = 0; i < 3; i++) Assert.That(ClinicStory.Continue(script, profile.story, profile.state), Is.True);
            Assert.That(ClinicStory.Answer(script, profile.story, profile.state, 0, "Sam"), Is.True);
            Assert.That(store.Save(profile, now), Is.True, store.Error);

            var reloaded = new ClinicProfileStore(directory).LoadClinic(now.AddSeconds(30));
            Assert.That(reloaded.story.line, Is.EqualTo(4));
            Assert.That(reloaded.story.bossName, Is.EqualTo("Sam"));
            Assert.That(reloaded.story.Choice("prologue.name"), Is.Zero);
            var beat = ClinicStory.Current(script, reloaded.story, reloaded.state);
            Assert.That(beat.Line.id, Is.EqualTo("prologue.start"));
            Assert.That(beat.Text, Is.EqualTo("Nice to meet you, Sam! Where do we start?"));
            Assert.That(reloaded.state.OpeningTaskEndsTick, Is.EqualTo(-1), "Waiting at the door costs nothing and starts nothing.");
        }

        [Test] public void ADamagedStoryIsRepairedAndNeverStopsTheClinicLoading()
        {
            var store = new ClinicProfileStore(directory);
            var profile = store.LoadClinic(now);
            WritePayload(PayloadWithStory(profile,
                "{\"enabled\":true,\"skipped\":false,\"skippedAt\":40,\"line\":-9,\"bossName\":\"<b>Bob</b> 7\",\"choices\":[\"\",\"broken\",\"prologue.start=1\"],\"pitchedInAt\":99}"));
            var loaded = new ClinicProfileStore(directory);
            var repaired = loaded.LoadClinic(now);
            Assert.That(loaded.Error, Is.Null, "The clinic loads normally; nothing is recovered from a backup.");
            Assert.That(repaired.story.enabled, Is.True);
            Assert.That(repaired.story.line, Is.Zero);
            Assert.That(repaired.story.skippedAt, Is.EqualTo(-1));
            Assert.That(repaired.story.pitchedInAt, Is.EqualTo((int)ClinicOpeningStep.Complete));
            Assert.That(repaired.story.bossName, Is.EqualTo("bBobb"));
            Assert.That(repaired.story.choices, Is.EqualTo(new[] { "prologue.start=1" }));
            Assert.That(loaded.Save(repaired, now), Is.True, loaded.Error);
        }

        [Test] public void TheShippedScriptCoversTheWholeOpening()
        {
            var script = Shipped();
            Assert.That(script.speaker, Is.EqualTo("Maya"));
            Assert.That(script.prologue.Length, Is.EqualTo(5));
            Assert.That(script.prologue.Count(l => l.askName), Is.EqualTo(1));
            var ask = script.prologue.Single(l => l.askName);
            Assert.That(ask.replies.Length, Is.EqualTo(2)); Assert.That(ask.said.Length, Is.EqualTo(2));
            Assert.That(script.prologue.Last().replies.Length, Is.GreaterThanOrEqualTo(1), "The prologue ends with where to start.");
            for (var step = ClinicOpeningStep.CleanOffice; step < ClinicOpeningStep.Complete; step++)
            {
                var lines = script.steps.Where(l => l.step == step.ToString()).ToArray();
                Assert.That(lines.Length, Is.EqualTo(1), step.ToString());
                var answers = step == ClinicOpeningStep.HireNurse || step == ClinicOpeningStep.HireReceptionist;
                Assert.That(lines[0].replies.Length > 0, Is.EqualTo(answers), step + ": only the hires are answered.");
            }
            var all = script.prologue.Concat(script.steps).Concat(script.reactions).Concat(script.lessons).ToArray();
            Assert.That(all.Select(l => l.id).Distinct().Count(), Is.EqualTo(all.Length), "Ids are stored with answers: unique.");
            foreach (var reaction in script.reactions)
                Assert.That(Enum.TryParse<ClinicOpeningStep>(reaction.step ?? "", out _) || reaction.on == ClinicStory.PitchInReaction, Is.True, reaction.id);
            foreach (var lesson in script.lessons) Assert.That(ClinicGuide.Find(lesson.id), Is.Not.Null, lesson.id);
            var text = File.ReadAllText(Path.Combine(Application.dataPath, "IdleClinic/Resources/ClinicStory.json"));
            Assert.That(text.IndexOf('—'), Is.EqualTo(-1), "No em dashes in the copy.");
            Assert.That(Regex.Matches(text, "\\{(\\w+)\\}").Cast<Match>().Select(m => m.Groups[1].Value).Distinct(), Is.EqualTo(new[] { "name" }));
            Assert.That(Regex.Matches(text, "</?(\\w+)>").Cast<Match>().Select(m => m.Groups[1].Value).Distinct(), Is.EqualTo(new[] { "i" }),
                "Only italics, which every label can show.");
        }

        [Test] public void ThePrologueIsQuickToReadAndEveryLineFitsTheCard()
        {
            var script = Shipped();
            var seconds = script.prologue.Sum(l => ClinicStory.ReadingSeconds(l.text, 200));
            Assert.That(seconds, Is.LessThanOrEqualTo(18), "About 15s at an unhurried 200 words a minute.");
            Assert.That(ClinicStory.ReadingSeconds(script.prologue[0].text + " " + script.prologue[1].text, 200), Is.LessThanOrEqualTo(8),
                "The first two lines take about as long as Maya's walk to the door and her knock.");
            foreach (var line in script.prologue.Concat(script.steps).Concat(script.reactions).Concat(script.lessons))
                Assert.That(ClinicStory.Format(line.text, "Alexandria Con").Length, Is.LessThanOrEqualTo(90), line.id);
        }
    }
}
