using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace IdleClinic.Core
{
    /// <summary>One line of the opening story. Ids are stored with the player's answers: never rename or reuse one.</summary>
    [Serializable]
    public sealed class ClinicStoryLine
    {
        public string id;
        /// <summary>A step line: the opening step it belongs to. A reaction: the step whose arrival it greets.</summary>
        public string step;
        /// <summary>A reaction to something other than reaching a step ("PitchIn").</summary>
        public string on;
        public string text;
        /// <summary>The line asks for the boss's name. The first reply keeps the typed name; any other keeps "boss".</summary>
        public bool askName;
        /// <summary>Answer buttons. On a step line they also start that step's task.</summary>
        public string[] replies;
        /// <summary>What the story log records for each answer, when it differs from the button.</summary>
        public string[] said;
    }

    /// <summary>The opening story as data (Resources/ClinicStory.json): Maya's prologue at the door, one line for each
    /// opening step, reactions to what just happened, and lines for the first guide lessons.</summary>
    [Serializable]
    public sealed class ClinicStoryScript
    {
        public string speaker;
        public ClinicStoryLine[] prologue, steps, reactions, lessons;

        /// <summary>Fill anything the data left out, so a damaged or partial file can never break the opening.</summary>
        public ClinicStoryScript Normalized()
        {
            if (string.IsNullOrEmpty(speaker)) speaker = "Maya";
            prologue = Clean(prologue); steps = Clean(steps); reactions = Clean(reactions); lessons = Clean(lessons);
            return this;
        }

        private static ClinicStoryLine[] Clean(ClinicStoryLine[] lines)
        {
            var kept = new List<ClinicStoryLine>();
            if (lines != null)
                foreach (var line in lines)
                {
                    if (line == null || string.IsNullOrEmpty(line.text)) continue;
                    line.replies = line.replies ?? Array.Empty<string>();
                    line.said = line.said ?? Array.Empty<string>();
                    kept.Add(line);
                }
            return kept.ToArray();
        }
    }

    /// <summary>The player's place in the opening story, saved with the profile. Only new games tell the story; saves from
    /// before it keep <see cref="enabled"/> false and play the opening exactly as before.</summary>
    [Serializable]
    public sealed class ClinicStoryState
    {
        public const int MaximumChoices = 16, MaximumIdLength = 64;
        public bool enabled;
        public bool skipped;
        /// <summary>The opening step when the player skipped the story, or -1.</summary>
        public int skippedAt = -1;
        /// <summary>Prologue lines read. The prologue is over once this reaches the number of prologue lines.</summary>
        public int line;
        /// <summary>The name the player chose; empty means "boss". Kept on this device only.</summary>
        public string bossName = "";
        /// <summary>Answers given, as "lineId=reply".</summary>
        public List<string> choices = new List<string>();
        /// <summary>The opening step when the player first pitched in, or -1.</summary>
        public int pitchedInAt = -1;

        public static ClinicStoryState NewGame() => new ClinicStoryState { enabled = true };

        /// <summary>Repair anything out of range instead of rejecting the save: the story never decides whether a clinic loads.</summary>
        public void Repair()
        {
            if (choices == null) choices = new List<string>();
            choices.RemoveAll(c => string.IsNullOrEmpty(c) || c.Length > MaximumIdLength || c.IndexOf('=') <= 0);
            if (choices.Count > MaximumChoices) choices.RemoveRange(MaximumChoices, choices.Count - MaximumChoices);
            line = Math.Max(0, Math.Min(line, 64));
            skippedAt = skipped ? Math.Max(0, Math.Min(skippedAt, (int)ClinicOpeningStep.Complete)) : -1;
            pitchedInAt = Math.Max(-1, Math.Min(pitchedInAt, (int)ClinicOpeningStep.Complete));
            bossName = ClinicStory.SanitizeName(bossName);
        }

        public int Choice(string id)
        {
            if (string.IsNullOrEmpty(id) || choices == null) return -1;
            var prefix = id + "=";
            foreach (var entry in choices)
                if (entry.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(entry.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                    return index;
            return -1;
        }

        internal void Choose(string id, int reply)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (choices == null) choices = new List<string>();
            choices.RemoveAll(c => c.StartsWith(id + "=", StringComparison.Ordinal));
            if (choices.Count < MaximumChoices) choices.Add(id + "=" + reply.ToString(CultureInfo.InvariantCulture));
        }
    }

    public enum ClinicStoryBeatKind { Prologue, Step }

    /// <summary>The line the story is on, ready to show: names filled in, answers listed.</summary>
    public sealed class ClinicStoryBeat
    {
        public ClinicStoryBeatKind Kind { get; }
        /// <summary>The prologue line number, or the opening step.</summary>
        public int Index { get; }
        public ClinicStoryLine Line { get; }
        public string Speaker { get; }
        public string Text { get; }
        public IReadOnlyList<string> Replies { get; }
        public bool AsksName => Line.askName;
        /// <summary>Answering this line ends the prologue and starts the first task.</summary>
        public bool EndsPrologue { get; }

        internal ClinicStoryBeat(ClinicStoryBeatKind kind, int index, ClinicStoryLine line, string speaker, string name, bool endsPrologue)
        {
            Kind = kind; Index = index; Line = line; Speaker = speaker; EndsPrologue = endsPrologue;
            Text = ClinicStory.Format(line.text, name);
            var replies = new string[line.replies.Length];
            for (var i = 0; i < replies.Length; i++) replies[i] = ClinicStory.Format(line.replies[i], name);
            Replies = replies;
        }
    }

    /// <summary>One line of the story so far, for the log in Settings.</summary>
    public readonly struct ClinicStoryEntry
    {
        public bool Boss { get; }
        public string Speaker { get; }
        public string Text { get; }
        public ClinicStoryEntry(bool boss, string speaker, string text) { Boss = boss; Speaker = speaker; Text = text; }
    }

    /// <summary>The opening story: Maya, a nurse, talks the boss (the player) through opening the clinic, then becomes the
    /// first nurse. Engine-free rules over <see cref="ClinicStoryScript"/> and <see cref="ClinicStoryState"/>; the game
    /// only shows them. Nothing here changes timers, prices or rewards.</summary>
    public static class ClinicStory
    {
        public const int MaximumNameLength = 14;
        public const string DefaultName = "boss";
        public const string PitchInReaction = "PitchIn";
        /// <summary>Maya becomes the nurse at the first nursing station.</summary>
        public static readonly int FirstNurseStaffId = ClinicRules.StaffId(ClinicStaffRole.Nurse, 0);
        /// <summary>Maya's look: chestnut hair swept into a bun.</summary>
        public const int FirstNurseAppearance = 2;

        /// <summary>A name the player typed, made safe to show: letters in any language with their accents, single spaces,
        /// hyphens and apostrophes, at most <see cref="MaximumNameLength"/> characters. Empty when no letter is left.</summary>
        /// <param name="shown">Optional: whether the game's fonts can draw a character. Characters they cannot are left out,
        /// so a name never appears as empty boxes.</param>
        public static string SanitizeName(string raw, Func<string, bool> shown = null)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string text;
            try { text = raw.Normalize(NormalizationForm.FormC); } catch (ArgumentException) { text = raw; }
            var result = new StringBuilder();
            int count = 0; bool letter = false, space = false;
            var elements = StringInfo.GetTextElementEnumerator(text);
            while (elements.MoveNext())
            {
                var element = (string)elements.Current;
                if (string.IsNullOrEmpty(element)) continue;
                if (char.IsWhiteSpace(element[0])) { space = result.Length > 0; continue; }
                bool isLetter = char.IsLetter(element, 0);
                bool joiner = element == "-" || element == "‐" || element == "'" || element == "’";
                if (!isLetter && !joiner || shown != null && !shown(element)) continue;
                if (count + (space ? 2 : 1) > MaximumNameLength) break;
                if (space) { result.Append(' '); count++; space = false; }
                result.Append(element); count++;
                letter |= isLetter;
            }
            return letter ? result.ToString() : "";
        }

        /// <summary>The name to show: the chosen name, or "boss".</summary>
        public static string DisplayName(string name) => string.IsNullOrEmpty(name) ? DefaultName : name;

        public static string Format(string text, string name) => (text ?? "").Replace("{name}", DisplayName(name));

        /// <summary>The story is on screen: a new game, not skipped, during the starter clinic's opening.</summary>
        public static bool Telling(ClinicStoryState story, ClinicState state)
            => story != null && story.enabled && !story.skipped && state != null && state.OpeningEnabled
                && state.Location == ClinicLocation.StarterClinic && state.OpeningStep != ClinicOpeningStep.Complete;

        /// <summary>Maya's prologue at the door is over (answered, or the clinic has moved on without it).</summary>
        public static bool PrologueDone(ClinicStoryScript script, ClinicStoryState story, ClinicState state)
            => story.line >= script.prologue.Length || state.OpeningStep > ClinicOpeningStep.CleanOffice || state.OpeningTaskEndsTick >= 0;

        public static ClinicStoryBeat Current(ClinicStoryScript script, ClinicStoryState story, ClinicState state)
        {
            if (script == null || !Telling(story, state)) return null;
            if (!PrologueDone(script, story, state))
                return new ClinicStoryBeat(ClinicStoryBeatKind.Prologue, story.line, script.prologue[story.line], script.speaker, story.bossName,
                    story.line == script.prologue.Length - 1);
            var line = StepLine(script, state.OpeningStep);
            return line == null ? null : new ClinicStoryBeat(ClinicStoryBeatKind.Step, (int)state.OpeningStep, line, script.speaker, story.bossName, false);
        }

        /// <summary>Move past a prologue line that needs no answer. False for any other line.</summary>
        public static bool Continue(ClinicStoryScript script, ClinicStoryState story, ClinicState state)
        {
            var beat = Current(script, story, state);
            if (beat == null || beat.Kind != ClinicStoryBeatKind.Prologue || beat.Replies.Count > 0) return false;
            story.line++;
            return true;
        }

        /// <summary>Answer the current line. Answering the name question with the first reply keeps <paramref name="typedName"/>
        /// (it must contain a letter); any other reply keeps "boss". After the last prologue line, or a step line, the caller
        /// starts the task the answer agreed to.</summary>
        public static bool Answer(ClinicStoryScript script, ClinicStoryState story, ClinicState state, int reply, string typedName = null)
        {
            var beat = Current(script, story, state);
            if (beat == null || reply < 0 || reply >= beat.Replies.Count) return false;
            if (beat.AsksName)
            {
                var name = reply == 0 ? SanitizeName(typedName) : "";
                if (reply == 0 && name.Length == 0) return false;
                story.bossName = name;
            }
            story.Choose(beat.Line.id, reply);
            if (beat.Kind == ClinicStoryBeatKind.Prologue) story.line = beat.EndsPrologue ? script.prologue.Length : story.line + 1;
            return true;
        }

        /// <summary>Stop telling the story. The opening carries on with plain step titles; Maya is still the first nurse.</summary>
        public static bool Skip(ClinicStoryState story, ClinicState state)
        {
            if (!Telling(story, state)) return false;
            story.skipped = true; story.skippedAt = (int)state.OpeningStep;
            return true;
        }

        /// <summary>Change the boss's name. An empty or letterless name means "boss".</summary>
        public static bool Rename(ClinicStoryState story, string raw)
        {
            if (story == null) return false;
            var name = SanitizeName(raw);
            if (name == story.bossName) return false;
            story.bossName = name;
            return true;
        }

        /// <summary>The player pitched in. True the first time, while the story is told: Maya has something to say.</summary>
        public static bool PitchIn(ClinicStoryState story, ClinicState state)
        {
            if (!Telling(story, state) || story.pitchedInAt >= 0) return false;
            story.pitchedInAt = (int)state.OpeningStep;
            return true;
        }

        /// <summary>Maya's reactions to the opening moving on from <paramref name="from"/> to <paramref name="to"/>, in order.</summary>
        public static IReadOnlyList<string> Reactions(ClinicStoryScript script, ClinicStoryState story, ClinicOpeningStep from, ClinicOpeningStep to)
        {
            var said = new List<string>();
            if (script == null || story == null || !story.enabled || story.skipped || to <= from) return said;
            foreach (var line in script.reactions)
                if (TryStep(line.step, out var step) && step > from && step <= to) said.Add(Format(line.text, story.bossName));
            return said;
        }

        public static string Reaction(ClinicStoryScript script, ClinicStoryState story, string on)
        {
            if (script == null || story == null || !story.enabled || story.skipped) return null;
            foreach (var line in script.reactions) if (line.on == on) return Format(line.text, story.bossName);
            return null;
        }

        /// <summary>Maya's line for a guide lesson after the opening, or null.</summary>
        public static string LessonLine(ClinicStoryScript script, ClinicStoryState story, string lessonId)
        {
            if (script == null || story == null || !story.enabled || story.skipped || lessonId == null) return null;
            foreach (var line in script.lessons) if (line.id == lessonId) return Format(line.text, story.bossName);
            return null;
        }

        /// <summary>The conversation so far, oldest first, using the current name. Lines after a skip are not included.</summary>
        public static IReadOnlyList<ClinicStoryEntry> Log(ClinicStoryScript script, ClinicStoryState story, ClinicState state)
        {
            var log = new List<ClinicStoryEntry>();
            if (script == null || story == null || !story.enabled || state == null) return log;
            bool toldPrologue = story.line >= script.prologue.Length;
            int shown = toldPrologue || !story.skipped && PrologueDone(script, story, state) ? script.prologue.Length : Math.Min(story.line + 1, script.prologue.Length);
            for (var i = 0; i < shown; i++) Add(log, script, story, script.prologue[i]);
            if (!toldPrologue && (story.skipped || !PrologueDone(script, story, state))) return log;
            var last = story.skipped ? (ClinicOpeningStep)story.skippedAt : state.OpeningEnabled ? state.OpeningStep : ClinicOpeningStep.Complete;
            for (var step = ClinicOpeningStep.CleanOffice; step <= last; step++)
            {
                foreach (var reaction in script.reactions)
                    if (TryStep(reaction.step, out var greets) && greets == step) log.Add(new ClinicStoryEntry(false, script.speaker, Format(reaction.text, story.bossName)));
                var line = StepLine(script, step);
                if (line != null) Add(log, script, story, line);
                if (story.pitchedInAt == (int)step)
                {
                    var pitch = Reaction(script, story, PitchInReaction);
                    if (pitch != null) log.Add(new ClinicStoryEntry(false, script.speaker, pitch));
                }
            }
            if (!story.skipped && last == ClinicOpeningStep.Complete && script.lessons.Length > 0)
                log.Add(new ClinicStoryEntry(false, script.speaker, Format(script.lessons[0].text, story.bossName)));
            return log;
        }

        /// <summary>Seconds to read <paramref name="text"/> at <paramref name="wordsPerMinute"/>.</summary>
        public static double ReadingSeconds(string text, double wordsPerMinute)
        {
            if (string.IsNullOrWhiteSpace(text) || wordsPerMinute <= 0) return 0;
            var words = text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return words * 60.0 / wordsPerMinute;
        }

        /// <summary>The starter clinic's first nurse is Maya in games that told the story (new games from 4.3 (23)).</summary>
        public static bool NamesFirstNurse(ClinicStoryState story, ClinicState state, int staffId)
            => story != null && story.enabled && state != null && state.Location == ClinicLocation.StarterClinic && staffId == FirstNurseStaffId;

        public static ClinicStoryLine StepLine(ClinicStoryScript script, ClinicOpeningStep step)
        {
            foreach (var line in script.steps) if (TryStep(line.step, out var value) && value == step) return line;
            return null;
        }

        /// <summary>What the boss says aloud for an answer: the line's "said" text when it has one, else the button.</summary>
        public static string Said(ClinicStoryLine line, int reply, string name)
        {
            if (line == null || line.replies == null || reply < 0 || reply >= line.replies.Length) return null;
            var said = line.said != null && reply < line.said.Length && !string.IsNullOrEmpty(line.said[reply]) ? line.said[reply] : line.replies[reply];
            return Format(said, name);
        }

        private static void Add(List<ClinicStoryEntry> log, ClinicStoryScript script, ClinicStoryState story, ClinicStoryLine line)
        {
            log.Add(new ClinicStoryEntry(false, script.speaker, Format(line.text, story.bossName)));
            var said = Said(line, story.Choice(line.id), story.bossName);
            if (said != null) log.Add(new ClinicStoryEntry(true, "You", said));
        }

        private static bool TryStep(string name, out ClinicOpeningStep step)
        {
            step = default;
            return !string.IsNullOrEmpty(name) && Enum.TryParse(name, false, out step) && Enum.IsDefined(typeof(ClinicOpeningStep), step);
        }
    }
}
