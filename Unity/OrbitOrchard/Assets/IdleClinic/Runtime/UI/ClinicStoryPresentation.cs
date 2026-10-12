using System;
using System.Collections.Generic;
using IdleClinic.Core;
using IdleClinic.Presentation;
using IdleClinic.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>The opening story on screen: Maya's lines in the guide card and the opening panel, answers that start the
    /// task they agree to, the boss's name, a speech bubble over Maya, her remarks as the clinic comes together, and the
    /// story so far in Settings. The rules live in <see cref="ClinicStory"/>; nothing here changes timers or rewards.</summary>
    public sealed partial class ClinicApp
    {
        private static readonly Color StoryInk = new Color(.13f, .38f, .39f);
        private ClinicStoryScript storyScript;
        private ClinicStoryBeat storyBeat;
        private Button storySkip;
        private ClinicIcon storyGlyph;
        private VisualElement storyBubble, bossBubble;
        private Label storyBubbleText, bossBubbleText;
        /// <summary>Something said aloud in a speech bubble: Maya's remarks, and the boss's answers.</summary>
        private struct StoryRemark { public bool Boss; public string Text; }
        private readonly Queue<StoryRemark> storyRemarks = new Queue<StoryRemark>();
        private StoryRemark? storyRemarkShowing;
        private double storyReactionUntil;
        private string storyCameraKey;
        private int storyObservedStep = -1;
        private bool storyPageOpen;
        private string storyNameDraft;

        private ClinicStoryScript StoryScript => storyScript ?? (storyScript = ClinicStoryScripts.Current);
        private ClinicStoryState Story => profile?.story;
        private string BossName => ClinicStory.DisplayName(Story?.bossName);

        private void BuildStory()
        {
            // Maya's face replaces the flag while she is talking.
            storyGlyph = new ClinicIcon(ClinicGlyph.Nurse, 22, StoryInk) { pickingMode = PickingMode.Ignore };
            storyGlyph.style.width = 22; storyGlyph.style.height = 22; storyGlyph.style.display = DisplayStyle.None;
            guideGlyph.parent.Add(storyGlyph);
            storySkip = IconButton(root, ClinicGlyph.Close, "Skip the story. The opening carries on without it", OnStorySkip, "guide-skip story-skip");
            storySkip.name = "story-skip";
            storySkip.RemoveFromClassList(Button.ussClassName);
            var icon = storySkip.Q<ClinicIcon>(); icon.style.width = 16; icon.style.height = 16;
            Text(storySkip, "Skip story", "guide-skip-label", true);
            storySkip.style.display = DisplayStyle.None;
            storyBubble = Bubble("story-bubble", out storyBubbleText);
            bossBubble = Bubble("boss-bubble", out bossBubbleText);
            bossBubble.AddToClassList("story-bubble-boss");
        }

        private VisualElement Bubble(string name, out Label text)
        {
            var bubble = Box(overlay, "story-bubble"); bubble.name = name; bubble.pickingMode = PickingMode.Ignore;
            text = Text(bubble, "", "story-bubble-text", true);
            Box(bubble, "story-bubble-tail").pickingMode = PickingMode.Ignore;
            bubble.style.display = DisplayStyle.None;
            return bubble;
        }

        /// <summary>Every readout tick: the current line, Maya's remarks on what just happened, and her place in the world.</summary>
        private void UpdateStory()
        {
            storyBeat = profile == null || simulation == null ? null : ClinicStory.Current(StoryScript, Story, State);
            ObserveStory();
            if (world != null && simulation != null) world.SetStoryCast(StoryCast());
        }

        private ClinicStoryCast StoryCast()
        {
            var story = Story;
            if (story == null || !story.enabled || State.Location != ClinicLocation.StarterClinic) return default;
            var script = StoryScript;
            var told = ClinicStory.Telling(story, State);
            return new ClinicStoryCast
            {
                Enabled = true, StaffId = ClinicStory.FirstNurseStaffId, Appearance = ClinicStory.FirstNurseAppearance,
                Arrived = story.line > 0 || story.skipped || ClinicStory.PrologueDone(script, story, State),
                Greeting = told && !ClinicStory.PrologueDone(script, story, State),
                KeepTogether = told,
            };
        }

        /// <summary>Queue Maya's remarks when the opening moves on while the player watches (not for steps passed while away).</summary>
        private void ObserveStory()
        {
            if (profile == null || simulation == null || State.Location != ClinicLocation.StarterClinic) { storyObservedStep = -1; return; }
            var step = State.OpeningEnabled ? State.OpeningStep : ClinicOpeningStep.Complete;
            if (storyObservedStep >= 0 && (int)step > storyObservedStep)
                foreach (var line in ClinicStory.Reactions(StoryScript, Story, (ClinicOpeningStep)storyObservedStep, step)) Say(false, line);
            storyObservedStep = (int)step;
        }

        private void ForgetStoryMoment() { storyObservedStep = -1; storyRemarks.Clear(); storyRemarkShowing = null; }

        /// <summary>Queue something said aloud: shown in turn, Maya's over her head and the boss's over theirs.</summary>
        private void Say(bool boss, string text)
        {
            if (!string.IsNullOrEmpty(text)) storyRemarks.Enqueue(new StoryRemark { Boss = boss, Text = text });
        }
        /// <summary>The remarks still to be shown, oldest first (for tests and accessibility).</summary>
        private List<string> QueuedStoryRemarks()
        {
            var texts = new List<string>();
            foreach (var remark in storyRemarks) texts.Add(remark.Text);
            return texts;
        }

        /// <summary>Per frame: speech bubbles. The latest remark for a few seconds over whoever said it; otherwise "…" over
        /// Maya while she has a line.</summary>
        private void UpdateStoryBubble()
        {
            if (storyBubble == null || world == null) return;
            var now = Time.unscaledTimeAsDouble;
            if (storyRemarkShowing != null && now >= storyReactionUntil) storyRemarkShowing = null;
            if (storyRemarkShowing == null && storyRemarks.Count > 0)
            {
                var next = storyRemarks.Dequeue();
                storyRemarkShowing = next;
                storyReactionUntil = now + Math.Max(next.Boss ? 2.4 : 3.2, ClinicStory.ReadingSeconds(next.Text, 180) + 1.5);
            }
            var showing = storyRemarkShowing;
            var sheet = settingsOpen || gemsOpen || locationsOpen || GemConfirmOpen;
            var maya = showing.HasValue ? (showing.Value.Boss ? null : showing.Value.Text) : storyBeat != null ? "…" : null;
            var boss = showing.HasValue && showing.Value.Boss ? showing.Value.Text : null;
            PlaceBubble(storyBubble, storyBubbleText, sheet ? null : maya, !showing.HasValue, world.TryGetStoryNursePoint(out var mayaHead), mayaHead);
            PlaceBubble(bossBubble, bossBubbleText, sheet ? null : boss, false, world.TryGetBossPoint(out var bossHead), bossHead);
        }

        private void PlaceBubble(VisualElement bubble, Label label, string text, bool waiting, bool found, Vector3 head)
        {
            if (text == null || !found) { bubble.style.display = DisplayStyle.None; return; }
            if (label.text != text) { label.text = text; bubble.EnableInClassList("story-bubble-waiting", waiting); }
            var uv = world.WorldToViewport(head);
            if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) { bubble.style.display = DisplayStyle.None; return; }
            bubble.style.display = DisplayStyle.Flex;
            var p = OverlayPoint(uv);
            var width = float.IsNaN(bubble.resolvedStyle.width) ? 60 : bubble.resolvedStyle.width;
            var height = float.IsNaN(bubble.resolvedStyle.height) ? 36 : bubble.resolvedStyle.height;
            bubble.style.left = p.x - width / 2; bubble.style.top = p.y - height - 6;
        }

        /// <summary>Per frame: while the story is told, the camera holds a close view of the boss and Maya together, above the
        /// card or panel. Each new line brings it back if the player has looked around; once the clinic is open (or the story is
        /// skipped) it eases back to the whole clinic.</summary>
        private void UpdateStoryCamera()
        {
            if (world == null) return;
            var beat = storyBeat;
            if (beat == null || State.Location != ClinicLocation.StarterClinic) { storyCameraKey = null; world.StopStoryFollow(); return; }
            var panel = dock.style.display == DisplayStyle.Flex;
            var prologue = beat.Kind == ClinicStoryBeatKind.Prologue;
            var queue = State.OpeningStep == ClinicOpeningStep.QueueArrivals || State.OpeningStep == ClinicOpeningStep.HireReceptionist;
            // While the first patients line up, a wider view that takes in the queue too.
            world.FollowStory(prologue ? 1.9f : 2.4f, panel ? .70f : queue ? .60f : .63f, panel ? .36f : queue ? .56f : .46f, !ReducedMotion, queue);
            var key = beat.Kind + ":" + beat.Index + ":" + beat.Line.id + ":" + panel;
            if (key != storyCameraKey) { storyCameraKey = key; world.ResumeStoryFollow(); }
        }

        /// <summary>The guide card while Maya is talking. False when the plain opening card should show.</summary>
        private bool UpdateStoryCard(bool busy)
        {
            var beat = storyBeat;
            var story = beat != null;
            guideCard.EnableInClassList("story-card", story);
            guideGlyph.style.display = story ? DisplayStyle.None : DisplayStyle.Flex;
            if (storyGlyph != null) storyGlyph.style.display = story ? DisplayStyle.Flex : DisplayStyle.None;
            ShowStorySkip(story && guideCard.style.display == DisplayStyle.Flex);
            if (!story) return false;
            var step = State.OpeningStep;
            guideEyebrow.text = beat.Speaker.ToUpperInvariant() + (beat.Kind == ClinicStoryBeatKind.Step ? " · " + ((int)step + 1) + " OF " + (int)ClinicOpeningStep.Complete : "");
            guideTitle.text = "“" + beat.Text + "”";
            guideWhy.text = StoryPrompt(beat, busy);
            return true;
        }

        private string StoryPrompt(ClinicStoryBeat beat, bool busy)
        {
            var step = State.OpeningStep;
            if (beat.Kind == ClinicStoryBeatKind.Prologue) return beat.Replies.Count > 0 ? "Tap to answer" : "Tap to continue";
            if (busy) return (ClinicSimulation.OpeningCleaning(step) ? "You’re cleaning" : "You’re setting up") + " · ready in "
                + TimeLabel((State.OpeningTaskEndsTick - State.Tick) / (double)ClinicRules.TicksPerSecond) + " · tap to pitch in";
            if (step == ClinicOpeningStep.FirstCheckIn || step == ClinicOpeningStep.QueueArrivals) return OpeningReasons[(int)step];
            return OpeningTitles[(int)step] + (beat.Replies.Count > 0 ? " · tap to answer" : " · tap to start");
        }

        private string StoryAccessibleValue()
        {
            var beat = storyBeat;
            return beat == null ? null : beat.Speaker + " says: " + PlainText(beat.Text) + " " + StoryPrompt(beat, State.OpeningTaskEndsTick >= 0) + ".";
        }

        /// <summary>A tap on the card moves past a prologue line that needs no answer. False otherwise.</summary>
        private bool TryContinueStory()
        {
            if (storyBeat == null || !ClinicStory.Continue(StoryScript, Story, State)) return false;
            SaveNow(); UpdateStory(); dockKey = ""; UpdateReadouts();
            return true;
        }

        /// <summary>Maya's line at the top of the opening panel, so the conversation stays visible while the card is hidden.</summary>
        private void BuildStoryQuote(VisualElement parent, ClinicStoryBeat beat)
        {
            var quote = Box(parent, "story-quote"); quote.name = "story-quote"; quote.pickingMode = PickingMode.Ignore;
            var tile = Box(quote, "icon-tile story-tile"); tile.pickingMode = PickingMode.Ignore;
            tile.Add(new ClinicIcon(ClinicGlyph.Nurse, 22, StoryInk) { pickingMode = PickingMode.Ignore });
            var words = Box(quote, "story-words"); words.pickingMode = PickingMode.Ignore;
            Text(words, beat.Speaker.ToUpperInvariant(), "eyebrow story-speaker", true);
            Text(words, "“" + beat.Text + "”", "story-line");
        }

        /// <summary>The prologue in the opening panel: continue, the name question, or where to start.</summary>
        private void BuildPrologueDock(ClinicStoryBeat beat)
        {
            if (beat.AsksName) { BuildNameQuestion(beat); return; }
            if (beat.Replies.Count == 0)
            {
                var next = StoryButton(dock, "Continue", "Continue", () => { if (TryContinueStory()) OpenOpeningTask(); }, "track-action primary-action");
                next.name = "story-continue";
                return;
            }
            for (var i = 0; i < beat.Replies.Count; i++)
            {
                var reply = i;
                StoryButton(dock, beat.Replies[i], "Answer: " + PlainText(beat.Replies[i]), () => AnswerStory(reply), "track-action primary-action story-reply").name = "story-reply-" + i;
            }
        }

        private void BuildNameQuestion(ClinicStoryBeat beat)
        {
            Text(dock, "Your name", "story-field-label", true);
            var field = new TextField { name = "story-name", maxLength = ClinicStory.MaximumNameLength, value = storyNameDraft ?? Story.bossName };
            field.AddToClassList("story-name-field");
            dock.Add(field);
            var preview = Text(dock, "", "story-name-preview");
            var confirm = StoryButton(dock, beat.Replies[0], "Answer with your name", () => AnswerStory(0, field.value), "track-action primary-action story-reply");
            confirm.name = "story-reply-0";
            if (beat.Replies.Count > 1)
                StoryButton(dock, beat.Replies[1], "Answer: " + PlainText(beat.Replies[1]), () => AnswerStory(1), "track-action story-reply story-reply-soft").name = "story-reply-1";
            void Refresh(string typed)
            {
                var name = ShownName(typed);
                preview.text = name.Length > 0 ? "Maya will call you " + name + "." : "Letters, spaces, hyphens and apostrophes. Up to " + ClinicStory.MaximumNameLength + ".";
                confirm.SetEnabled(name.Length > 0);
            }
            field.RegisterValueChangedCallback(change => { storyNameDraft = change.newValue; Refresh(change.newValue); });
            field.RegisterCallback<KeyDownEvent>(key => { if (key.keyCode == KeyCode.Return || key.keyCode == KeyCode.KeypadEnter) AnswerStory(0, field.value); });
            RegisterAccessibleButton(field, "Your name, text field", () => field.Focus(), () => field.value);
            Refresh(field.value);
        }

        /// <summary>A step line with answers (hiring Maya, the receptionist): each answer starts the step's task.</summary>
        private void BuildStepAnswers(ClinicStoryBeat beat)
        {
            for (var i = 0; i < beat.Replies.Count; i++)
            {
                var reply = i;
                var button = StoryButton(dock, beat.Replies[i], PlainText(beat.Replies[i]), () => AnswerStory(reply), "track-action primary-action story-reply");
                button.name = i == 0 ? "opening-action" : "story-reply-" + i;
            }
        }

        private void AnswerStory(int reply, string typedName = null)
        {
            var beat = storyBeat;
            if (beat == null || saves != null && saves.HasPendingOfflineProgress) return;
            var step = State.OpeningStep;
            if (!ClinicStory.Answer(StoryScript, Story, State, reply, beat.AsksName && reply == 0 ? ShownName(typedName) : typedName))
            {
                if (beat.AsksName) Notify("Type a name with at least one letter, or choose “" + PlainText(beat.Replies[Math.Min(1, beat.Replies.Count - 1)]) + "”.", 5);
                return;
            }
            if (beat.AsksName) storyNameDraft = null;
            Feedback(0);
            Say(true, ClinicStory.Said(beat.Line, reply, Story.bossName));
            // "Where do we start?" The boss and Maya walk from the door to the office together, so the first task starts there.
            if (beat.EndsPrologue) { Run(() => simulation.StartOpeningAtEntrance(), true); UpdateStory(); return; }
            if (beat.Kind == ClinicStoryBeatKind.Step) { Run(() => simulation.ContinueOpening(step), true); UpdateStory(); return; }
            SaveNow(); UpdateStory(); dockKey = ""; UpdateReadouts();
        }

        private void OnStorySkip()
        {
            if (!ClinicStory.Skip(Story, State)) return;
            SaveNow(); UpdateStory(); dockKey = ""; UpdateReadouts();
            Notify("Story skipped. You can read it so far in Settings › Story.", 5);
        }

        /// <summary>Back to the plain guide card once the opening is over or the story is not being told.</summary>
        private void ResetStoryCard()
        {
            if (guideCard == null) return;
            guideCard.RemoveFromClassList("story-card");
            guideGlyph.style.display = DisplayStyle.Flex;
            if (storyGlyph != null) storyGlyph.style.display = DisplayStyle.None;
            ShowStorySkip(false);
        }

        private void ShowStorySkip(bool show)
        {
            if (storySkip == null) return;
            var display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (storySkip.style.display == display) return;
            storySkip.style.display = display; ApplySafeArea();
        }

        /// <summary>Pitching in: the first time while Maya is talking, she has something to say about it.</summary>
        private void StoryPitchedIn()
        {
            if (!ClinicStory.PitchIn(Story, State)) return;
            Say(false, ClinicStory.Reaction(StoryScript, Story, ClinicStory.PitchInReaction));
        }

        /// <summary>The name as the game can show it: rules from <see cref="ClinicStory.SanitizeName"/>, and only characters
        /// every interface font can draw.</summary>
        private string ShownName(string typed) => ClinicStory.SanitizeName(typed, Drawable);
        private bool Drawable(string element)
        {
            foreach (var font in new[] { bodyFont, boldFont, displayFont })
            {
                if (font == null) continue;
                foreach (var character in element)
                {
                    if (char.IsSurrogate(character)) return false;
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
                    if (!font.HasCharacter(character)) return false;
                }
            }
            return true;
        }

        private static string PlainText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text ?? "";
            var plain = new System.Text.StringBuilder(text.Length);
            var inTag = false;
            foreach (var c in text) { if (c == '<') inTag = true; else if (c == '>') inTag = false; else if (!inTag) plain.Append(c); }
            return plain.ToString();
        }

        private Button StoryButton(VisualElement parent, string label, string accessible, Action action, string classes)
        {
            var button = IconButton(parent, ClinicGlyph.Chevron, accessible, action, classes);
            var icon = button.Q<ClinicIcon>(); icon.RemoveFromHierarchy();
            Text(button, label, "action-label story-reply-label", true);
            button.Add(icon);
            return button;
        }

        // ---- Settings › Story --------------------------------------------------------------------------------------

        private void BuildStorySettingsLink(VisualElement links)
        {
            if (Story == null || !Story.enabled) return;
            var button = IconButton(links, ClinicGlyph.Nurse, "Story. Your name and the story so far", OpenStoryPage, "settings-link");
            button.name = "settings-story";
            Text(button, "Story", "settings-link-label", true);
            Text(button, BossName, "settings-link-value");
            button.Add(new ClinicIcon(ClinicGlyph.Chevron, 16));
        }

        private void OpenStoryPage() { storyPageOpen = true; storyNameDraft = null; dockKey = ""; UpdateReadouts(); }
        private void CloseStoryPage() { storyPageOpen = false; dockKey = ""; UpdateReadouts(); }

        private void BuildStoryPage(VisualElement content)
        {
            var back = IconButton(content, ClinicGlyph.Chevron, "Back to settings", CloseStoryPage, "settings-link story-back");
            back.name = "story-back";
            back.Q<ClinicIcon>().style.rotate = new Rotate(180);
            Text(back, "Settings", "settings-link-label", true);
            var naming = Box(content, "story-naming");
            Text(naming, "Your name", "story-field-label", true);
            var row = Box(naming, "story-name-row");
            var field = new TextField { name = "story-rename", maxLength = ClinicStory.MaximumNameLength, value = storyNameDraft ?? Story.bossName };
            field.AddToClassList("story-name-field");
            row.Add(field);
            var save = IconButton(row, ClinicGlyph.Check, "Save your name", () => RenameBoss(field.value), "story-save");
            save.name = "story-save";
            Text(save, "Save", "action-label", true);
            var preview = Text(naming, "", "story-name-preview");
            void Refresh(string typed)
            {
                var name = ShownName(typed);
                preview.text = name.Length > 0 ? "Maya calls you " + name + "." : "Leave it empty and Maya calls you boss.";
                save.SetEnabled(name != Story.bossName);
            }
            field.RegisterValueChangedCallback(change => { storyNameDraft = change.newValue; Refresh(change.newValue); });
            RegisterAccessibleButton(field, "Your name, text field", () => field.Focus(), () => field.value);
            Refresh(field.value);
            Text(content, "THE STORY SO FAR", "eyebrow story-log-heading", true);
            var log = Box(content, "story-log"); log.name = "story-log"; log.pickingMode = PickingMode.Ignore;
            var entries = ClinicStory.Log(StoryScript, Story, profile.state);
            foreach (var entry in entries)
            {
                var bubble = Box(log, entry.Boss ? "story-entry story-entry-boss" : "story-entry"); bubble.pickingMode = PickingMode.Ignore;
                Text(bubble, entry.Boss ? BossName : entry.Speaker, "story-entry-speaker", true);
                Text(bubble, entry.Text, "story-entry-text");
            }
            if (entries.Count == 0) Text(log, "The story starts when Maya knocks on your door.", "help-text");
            if (Story.skipped) Text(content, "You skipped the rest of the story.", "help-text story-log-note");
        }

        private void RenameBoss(string typed)
        {
            if (!ClinicStory.Rename(Story, ShownName(typed))) return;
            storyNameDraft = null;
            SaveNow(); UpdateStory(); dockKey = ""; UpdateReadouts();
            Notify("Maya will call you " + BossName + ".", 4);
        }

        /// <summary>Maya's name in her station panel, in games that told the story.</summary>
        private string StaffName(int staffId) => ClinicStory.NamesFirstNurse(Story, State, staffId) ? StoryScript.speaker : null;
    }
}
