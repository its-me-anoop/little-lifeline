using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The opening story's rules: Maya's prologue, answering and naming, one line per step, reactions, skipping,
    /// relaunching mid-story and the log. The story never changes the opening's timers or rewards.</summary>
    public sealed class ClinicStoryTests
    {
        private static ClinicStoryLine L(string id, string text, string step = null, string on = null, bool askName = false, string[] replies = null, string[] said = null)
            => new ClinicStoryLine { id = id, text = text, step = step, on = on, askName = askName, replies = replies, said = said };

        internal static ClinicStoryScript Script() => new ClinicStoryScript
        {
            speaker = "Maya",
            prologue = new[]
            {
                L("p1", "Hello? I'm Maya."),
                L("p2", "Sorry, I didn't catch your name?", askName: true, replies: new[] { "That's me", "Just call me boss" }, said: new[] { "I'm {name}.", "Just call me boss." }),
                L("p3", "Nice to meet you, {name}! Where do we start?", replies: new[] { "My office.", "Biscuits first." }),
            },
            steps = new[]
            {
                L("s.office", "Is that a box of… more boxes?", step: "CleanOffice"),
                L("s.desk", "And a bell.", step: "SetUpReception"),
                L("s.nurse", "So… about that job?", step: "HireNurse", replies: new[] { "You're hired, Maya.", "Welcome aboard, Maya." }),
            },
            reactions = new[]
            {
                L("r.ding", "Ding! Sorry. Had to.", step: "FirstCheckIn"),
                L("r.thanks", "Thanks, {name}.", step: "QueueArrivals"),
                L("r.queue", "Before they start a queue for the queue.", step: "Complete"),
                L("r.open", "We're open, {name}.", step: "Complete"),
                L("r.pitch", "Look at you, sleeves rolled up!", on: ClinicStory.PitchInReaction),
            },
            lessons = new[] { L("guide.equipment", "Better kit means quicker care.") },
        }.Normalized();

        private static ClinicState Opening() => ClinicSimulation.CreateOpening().State;

        [Test] public void ANewGameStartsAtMayasFirstLineAndOldSavesHaveNoStory()
        {
            var state = Opening();
            var beat = ClinicStory.Current(Script(), ClinicStoryState.NewGame(), state);
            Assert.That(beat.Kind, Is.EqualTo(ClinicStoryBeatKind.Prologue));
            Assert.That(beat.Text, Is.EqualTo("Hello? I'm Maya."));
            Assert.That(beat.Speaker, Is.EqualTo("Maya"));
            Assert.That(ClinicStory.Current(Script(), new ClinicStoryState(), state), Is.Null, "A save from before the story keeps the plain opening.");
            Assert.That(ClinicStory.Current(Script(), null, state), Is.Null);
        }

        [Test] public void ThePrologueWalksLineByLineAsksTheNameAndEndsWithAnAnswer()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            Assert.That(ClinicStory.Answer(script, story, state, 0), Is.False, "The first line has no answers.");
            Assert.That(ClinicStory.Continue(script, story, state), Is.True);
            var ask = ClinicStory.Current(script, story, state);
            Assert.That(ask.AsksName, Is.True);
            Assert.That(ClinicStory.Continue(script, story, state), Is.False, "A question waits for an answer.");
            Assert.That(ClinicStory.Answer(script, story, state, 0, "  12 "), Is.False, "A name needs a letter.");
            Assert.That(ClinicStory.Answer(script, story, state, 0, "Sam"), Is.True);
            Assert.That(story.bossName, Is.EqualTo("Sam"));
            var last = ClinicStory.Current(script, story, state);
            Assert.That(last.Text, Is.EqualTo("Nice to meet you, Sam! Where do we start?"));
            Assert.That(last.EndsPrologue, Is.True);
            Assert.That(ClinicStory.Answer(script, story, state, 1), Is.True);
            Assert.That(ClinicStory.PrologueDone(script, story, state), Is.True);
            var step = ClinicStory.Current(script, story, state);
            Assert.That(step.Kind, Is.EqualTo(ClinicStoryBeatKind.Step));
            Assert.That(step.Text, Is.EqualTo("Is that a box of… more boxes?"));
            Assert.That(state.OpeningStep, Is.EqualTo(ClinicOpeningStep.CleanOffice), "Answering never moves the opening on by itself.");
        }

        [Test] public void JustCallMeBossKeepsTheDefaultName()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            ClinicStory.Continue(script, story, state);
            Assert.That(ClinicStory.Answer(script, story, state, 1, "ignored"), Is.True);
            Assert.That(story.bossName, Is.Empty);
            Assert.That(ClinicStory.Current(script, story, state).Text, Is.EqualTo("Nice to meet you, boss! Where do we start?"));
        }

        [TestCase("Sam", "Sam")]
        [TestCase("  Mary   Jane  ", "Mary Jane")]
        [TestCase("O'Neil-Smith", "O'Neil-Smith")]
        [TestCase("Zoë", "Zoë")]
        [TestCase("Zoë", "Zoë")]
        [TestCase("José María", "José María")]
        [TestCase("Анна", "Анна")]
        [TestCase("さくら", "さくら")]
        [TestCase("محمد", "محمد")]
        [TestCase("Sam123!", "Sam")]
        [TestCase("<b>Sam</b>", "bSamb")]
        [TestCase("Alexandria Constance", "Alexandria Con")]
        [TestCase("Sam 🙂", "Sam")]
        [TestCase("123", "")]
        [TestCase("--''", "")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void NamesKeepLettersInAnyLanguageSpacesHyphensAndApostrophes(string typed, string kept)
        {
            Assert.That(ClinicStory.SanitizeName(typed), Is.EqualTo(kept));
            Assert.That(new System.Globalization.StringInfo(ClinicStory.SanitizeName(typed)).LengthInTextElements, Is.LessThanOrEqualTo(ClinicStory.MaximumNameLength));
        }

        [Test] public void LettersTheFontsCannotDrawAreLeftOutRatherThanShownAsBoxes()
        {
            System.Func<string, bool> latin = element => element[0] < 0x0250;
            Assert.That(ClinicStory.SanitizeName("Zoë Анна", latin), Is.EqualTo("Zoë"));
            Assert.That(ClinicStory.SanitizeName("さくら", latin), Is.Empty, "Nothing drawable left: the player stays boss.");
        }

        [Test] public void EachStepHasItsOwnLineAndTheNameIsUsedEverywhere()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            story.line = script.prologue.Length; story.bossName = "Ana";
            state.OpeningStep = ClinicOpeningStep.SetUpReception;
            Assert.That(ClinicStory.Current(script, story, state).Text, Is.EqualTo("And a bell."));
            state.OpeningStep = ClinicOpeningStep.HireNurse;
            var hire = ClinicStory.Current(script, story, state);
            Assert.That(hire.Replies, Is.EqualTo(new[] { "You're hired, Maya.", "Welcome aboard, Maya." }));
            Assert.That(ClinicStory.Answer(script, story, state, 1), Is.True);
            Assert.That(story.Choice("s.nurse"), Is.EqualTo(1));
            Assert.That(ClinicStory.Reactions(script, story, ClinicOpeningStep.HireNurse, ClinicOpeningStep.QueueArrivals), Is.EqualTo(new[] { "Thanks, Ana." }));
            state.OpeningStep = ClinicOpeningStep.CleanWaiting;
            Assert.That(ClinicStory.Current(script, story, state), Is.Null, "A step without a line shows the plain task.");
        }

        [Test] public void ReactionsFollowTheOpeningInOrderAndOnlyOnce()
        {
            var script = Script(); var story = ClinicStoryState.NewGame();
            Assert.That(ClinicStory.Reactions(script, story, ClinicOpeningStep.SetUpReception, ClinicOpeningStep.FirstCheckIn), Is.EqualTo(new[] { "Ding! Sorry. Had to." }));
            Assert.That(ClinicStory.Reactions(script, story, ClinicOpeningStep.FirstCheckIn, ClinicOpeningStep.FirstCheckIn), Is.Empty);
            Assert.That(ClinicStory.Reactions(script, story, ClinicOpeningStep.HireReceptionist, ClinicOpeningStep.Complete),
                Is.EqualTo(new[] { "Before they start a queue for the queue.", "We're open, boss." }));
            story.skipped = true; story.skippedAt = 0;
            Assert.That(ClinicStory.Reactions(script, story, ClinicOpeningStep.SetUpReception, ClinicOpeningStep.FirstCheckIn), Is.Empty);
            Assert.That(ClinicStory.Reactions(script, new ClinicStoryState(), ClinicOpeningStep.SetUpReception, ClinicOpeningStep.FirstCheckIn), Is.Empty);
        }

        [Test] public void PitchingInDrawsOneRemarkOnly()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            story.line = script.prologue.Length;
            Assert.That(ClinicStory.PitchIn(story, state), Is.True);
            Assert.That(ClinicStory.Reaction(script, story, ClinicStory.PitchInReaction), Is.EqualTo("Look at you, sleeves rolled up!"));
            Assert.That(ClinicStory.PitchIn(story, state), Is.False);
            Assert.That(story.pitchedInAt, Is.EqualTo((int)ClinicOpeningStep.CleanOffice));
        }

        [Test] public void SkippingStopsTheStoryButKeepsMayaAsTheFirstNurse()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            Assert.That(ClinicStory.Skip(story, state), Is.True);
            Assert.That(ClinicStory.Current(script, story, state), Is.Null);
            Assert.That(ClinicStory.Skip(story, state), Is.False);
            Assert.That(story.bossName, Is.Empty, "Skipping keeps the player as the boss.");
            Assert.That(ClinicStory.NamesFirstNurse(story, state, ClinicRules.StaffId(ClinicStaffRole.Nurse, 0)), Is.True);
            Assert.That(ClinicStory.NamesFirstNurse(story, state, ClinicRules.StaffId(ClinicStaffRole.Nurse, 1)), Is.False);
            Assert.That(ClinicStory.NamesFirstNurse(new ClinicStoryState(), state, ClinicStory.FirstNurseStaffId), Is.False, "Older games never met Maya.");
            Assert.That(ClinicStory.LessonLine(script, story, "guide.equipment"), Is.Null);
        }

        [Test] public void RelaunchingMidStoryResumesAtTheSameLine()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            ClinicStory.Continue(script, story, state);
            var copy = new ClinicStoryState { enabled = story.enabled, line = story.line, bossName = story.bossName, choices = story.choices.ToList() };
            copy.Repair();
            Assert.That(ClinicStory.Current(script, copy, state).Line.id, Is.EqualTo("p2"));
            var task = Opening(); task.OpeningTaskEndsTick = 40;
            Assert.That(ClinicStory.Current(script, ClinicStoryState.NewGame(), task).Kind, Is.EqualTo(ClinicStoryBeatKind.Step),
                "A clinic already at work is past the door.");
        }

        [Test] public void TheStoryIsSilentOnceTheClinicIsOpenExceptForTheFirstLesson()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            state.OpeningStep = ClinicOpeningStep.Complete;
            Assert.That(ClinicStory.Current(script, story, state), Is.Null);
            Assert.That(ClinicStory.LessonLine(script, story, "guide.equipment"), Is.EqualTo("Better kit means quicker care."));
            Assert.That(ClinicStory.LessonLine(script, story, "guide.waiting"), Is.Null);
            state.Location = ClinicLocation.DoctorsClinic; state.OpeningStep = ClinicOpeningStep.CleanOffice;
            Assert.That(ClinicStory.Current(script, story, state), Is.Null, "The doctors clinic has no opening story.");
        }

        [Test] public void TheLogReplaysTheConversationWithTheCurrentName()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            ClinicStory.Continue(script, story, state);
            ClinicStory.Answer(script, story, state, 0, "Sam");
            ClinicStory.Answer(script, story, state, 0);
            state.OpeningStep = ClinicOpeningStep.FirstCheckIn;
            ClinicStory.Rename(story, "Priya");
            var log = ClinicStory.Log(script, story, state);
            Assert.That(log.Select(e => (e.Boss ? "You: " : "Maya: ") + e.Text), Is.EqualTo(new[]
            {
                "Maya: Hello? I'm Maya.", "Maya: Sorry, I didn't catch your name?", "You: I'm Priya.",
                "Maya: Nice to meet you, Priya! Where do we start?", "You: My office.",
                "Maya: Is that a box of… more boxes?", "Maya: And a bell.", "Maya: Ding! Sorry. Had to.",
            }));
        }

        [Test] public void TheLogStopsWhereThePlayerSkipped()
        {
            var script = Script(); var story = ClinicStoryState.NewGame(); var state = Opening();
            ClinicStory.Continue(script, story, state);
            ClinicStory.Skip(story, state);
            state.OpeningStep = ClinicOpeningStep.Complete;
            Assert.That(ClinicStory.Log(script, story, state).Select(e => e.Text), Is.EqualTo(new[] { "Hello? I'm Maya.", "Sorry, I didn't catch your name?" }));
            Assert.That(ClinicStory.Log(script, new ClinicStoryState(), state), Is.Empty);
        }

        [Test] public void DamagedStoryDataIsRepairedNeverRejected()
        {
            var story = new ClinicStoryState { enabled = true, line = -4, skippedAt = 99, pitchedInAt = 77, bossName = "<i>Bob</i> 42", choices = null };
            story.Repair();
            Assert.That(story.line, Is.Zero); Assert.That(story.skippedAt, Is.EqualTo(-1)); Assert.That(story.pitchedInAt, Is.EqualTo((int)ClinicOpeningStep.Complete));
            Assert.That(story.bossName, Is.EqualTo("iBobi"));
            Assert.That(story.choices, Is.Empty);
            story.choices.AddRange(Enumerable.Range(0, 40).Select(i => "x" + i + "=0").Concat(new[] { "", "broken" }));
            story.Repair();
            Assert.That(story.choices.Count, Is.EqualTo(ClinicStoryState.MaximumChoices));
            var broken = new ClinicStoryScript { prologue = new[] { null, new ClinicStoryLine { id = "x" } } }.Normalized();
            Assert.That(broken.prologue, Is.Empty); Assert.That(broken.steps, Is.Empty); Assert.That(broken.speaker, Is.EqualTo("Maya"));
            Assert.That(ClinicStory.Current(broken, ClinicStoryState.NewGame(), Opening()), Is.Null, "An empty script shows the plain opening.");
        }

        [Test] public void ReadingTimeCountsWords()
        {
            Assert.That(ClinicStory.ReadingSeconds("one two three four", 240), Is.EqualTo(1).Within(1e-9));
            Assert.That(ClinicStory.ReadingSeconds("", 240), Is.Zero);
        }
    }
}
