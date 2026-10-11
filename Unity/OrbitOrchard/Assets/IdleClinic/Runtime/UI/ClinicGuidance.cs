using System;
using IdleClinic.Core;
using IdleClinic.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>After the opening tutorial, one clear next step at a time: tap the card to go there, follow the
    /// highlighted control, then collect the gems it pays. The card stays out of the way while a panel is open.</summary>
    public sealed partial class ClinicApp
    {
        private Button guideCard;
        private VisualElement guidePointer;
        private Label guideEyebrow, guideTitle, guideWhy, guideReward;
        private ClinicIcon guideGlyph, guideRewardGem;
        private Button guideHighlighted;
        // Optional lessons (gem decor, finishing early) can be skipped: the guide then carries on with free lessons.
        private Button guideSkip;
        // When no lesson is left (or the long "open the doctors clinic" lesson is under way) the card names the next improvement
        // the player is saving for, how far away it is at their measured earning rate, or what the builder is finishing.
        private ClinicNextGoal nextGoal;
        private double nextGoalAt, earnSampleAt;
        private long earnSampleTotal = -1;
        private double coinsPerSecond;

        private void BuildGuide()
        {
            guideCard = IconButton(root, ClinicGlyph.Goal, "Next step", OnGuideTap, "guide-card", GuideAccessibleValue);
            guideCard.name = "clinic-guide";
            // The flag sits in its own tile; the reward is shown up front and becomes the button once the step is done.
            guideGlyph = guideCard.Q<ClinicIcon>(); guideGlyph.RemoveFromHierarchy();
            var tile = Box(guideCard, "icon-tile guide-tile"); tile.pickingMode = PickingMode.Ignore;
            guideGlyph.Tint = LeafInk; guideGlyph.style.width = 22; guideGlyph.style.height = 22; tile.Add(guideGlyph);
            var words = Box(guideCard, "guide-words"); words.pickingMode = PickingMode.Ignore;
            guideEyebrow = Text(words, "NEXT STEP", "eyebrow", true);
            guideTitle = Display(Text(words, "", "guide-title", true));
            guideWhy = Text(words, "", "guide-why");
            var reward = Box(guideCard, "guide-reward"); reward.pickingMode = PickingMode.Ignore;
            guideRewardGem = new ClinicIcon(ClinicGlyph.Gem, 18, GemInk); reward.Add(guideRewardGem);
            guideReward = Display(Text(reward, "", "guide-reward-value", true));
            guideCard.style.display = DisplayStyle.None;
            guidePointer = Box(overlay, "guide-pointer"); guidePointer.pickingMode = PickingMode.Ignore;
            var arrow = new ClinicIcon(ClinicGlyph.Arrow, 26, new Color(.98f, .96f, .90f)); arrow.style.rotate = new Rotate(90);
            guidePointer.Add(arrow);
            guidePointer.style.display = DisplayStyle.None;
            guideSkip = IconButton(root, ClinicGlyph.Close, "Skip this optional lesson and keep your gems", OnGuideSkip, "guide-skip");
            guideSkip.name = "guide-skip";
            guideSkip.RemoveFromClassList(Button.ussClassName);
            var skipIcon = guideSkip.Q<ClinicIcon>(); skipIcon.style.width = 16; skipIcon.style.height = 16;
            Text(guideSkip, "Skip · keep gems", "guide-skip-label", true);
            guideSkip.style.display = DisplayStyle.None;
        }

        private ClinicGuideStep CurrentGuide()
            => saves == null || State.Tutorial != ClinicTutorialStep.Complete ? null : saves.CurrentGuideStep();

        private string GuideAccessibleValue()
        {
            var step = CurrentGuide();
            if(State.OpeningEnabled && State.OpeningStep!=ClinicOpeningStep.Complete) return "Opening step "+((int)State.OpeningStep+1)+" of "+(int)ClinicOpeningStep.Complete+". "+OpeningTitles[(int)State.OpeningStep]+". "+OpeningReasons[(int)State.OpeningStep];
            if (nextGoal != null && ShowsNextGoal(step)) return guideEyebrow.text + ": " + guideTitle.text + ". " + guideWhy.text;
            if (step == null) return "";
            var place = ClinicGuide.Position(step);
            var lesson = place.Total > 0 ? "Lesson " + place.Number + " of " + place.Total + ". " : "";
            return lesson + (saves.IsGuideStepDone(step) ? step.Title + " done. Collect " + step.GemReward + " gems." : step.Title + ". " + step.Why);
        }

        private void UpdateGuide()
        {
            if (guideCard == null) return;
            if(State.OpeningEnabled && State.OpeningStep!=ClinicOpeningStep.Complete){ShowGuideSkip(false);UpdateOpeningGuide();return;}
            var step = CurrentGuide();
            var dockOpen = dock.style.display == DisplayStyle.Flex;
            if (ShowsNextGoal(step) && UpdateNextGoal())
            {
                ShowGuideSkip(false);
                var goalDisplay = dockOpen ? DisplayStyle.None : DisplayStyle.Flex;
                if (guideCard.style.display != goalDisplay) { guideCard.style.display = goalDisplay; ApplySafeArea(); }
                RenderNextGoal(step != null);
                if (guideHighlighted != null) { guideHighlighted.RemoveFromClassList("guide-target"); guideHighlighted = null; }
                return;
            }
            guideRewardGem.parent.style.display=DisplayStyle.Flex;
            var done = step != null && saves.IsGuideStepDone(step);
            var display = step == null || dockOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (guideCard.style.display != display) { guideCard.style.display = display; ApplySafeArea(); }
            ShowGuideSkip(display == DisplayStyle.Flex && ClinicGuide.CanSkip(step) && !done);
            if (step != null)
            {
                var position = ClinicGuide.Position(step);
                guideEyebrow.text = (done ? "DONE" : "NEXT STEP") + (position.Total > 0 ? " · " + position.Number + " OF " + position.Total : "");
                guideTitle.text = step.Title;
                guideWhy.text = done ? "Tap to collect your reward." : step.Why;
                guideReward.text = "+" + step.GemReward;
                if (guideCard.ClassListContains("guide-ready") != done)
                {
                    guideCard.EnableInClassList("guide-ready", done);
                    guideGlyph.Tint = done ? GemInk : LeafInk; guideGlyph.MarkDirtyRepaint();
                    guideRewardGem.Tint = done ? GemOnInk : GemInk; guideRewardGem.MarkDirtyRepaint();
                }
            }
            // Point at the exact control once its room or object is open.
            var target = step != null && !done && step.Control != null && dockOpen ? dock.Q<Button>(step.Control) : null;
            if (target != guideHighlighted)
            {
                guideHighlighted?.RemoveFromClassList("guide-target");
                target?.AddToClassList("guide-target");
                guideHighlighted = target;
            }
        }

        private bool ShowsNextGoal(ClinicGuideStep step)
            => State.Tutorial == ClinicTutorialStep.Complete && (step == null || step.Id == "guide.doctors" && !saves.IsGuideStepDone(step));

        /// <summary>Refresh the goal twice a second and the earning rate every ten seconds (smoothed). False when nothing is left.</summary>
        private bool UpdateNextGoal()
        {
            var now = Time.unscaledTimeAsDouble;
            if (earnSampleTotal < 0 || State.TotalEarned < earnSampleTotal) { earnSampleTotal = State.TotalEarned; earnSampleAt = now; coinsPerSecond = 0; }
            else if (now - earnSampleAt >= 10)
            {
                var rate = (State.TotalEarned - earnSampleTotal) / (now - earnSampleAt);
                coinsPerSecond = coinsPerSecond <= 0 ? rate : coinsPerSecond * .6 + rate * .4;
                earnSampleTotal = State.TotalEarned; earnSampleAt = now;
            }
            if (now >= nextGoalAt || nextGoal == null) { nextGoal = ClinicPurchases.NextGoal(simulation); nextGoalAt = now + .5; }
            return nextGoal != null;
        }

        /// <param name="towardsLesson">The open lesson is "open the doctors clinic": say that this goal leads there.</param>
        private void RenderNextGoal(bool towardsLesson)
        {
            guideRewardGem.parent.style.display = DisplayStyle.None;
            guidePointer.style.display = DisplayStyle.None;
            guideCard.EnableInClassList("guide-ready", false);
            var goal = nextGoal;
            guideTitle.text = goal.Label;
            if (goal.WaitingOnly)
            {
                guideEyebrow.text = "BUILDING";
                guideWhy.text = "Ready in " + TimeLabel(goal.BuilderFreeInSeconds) + ". Your clinic keeps earning meanwhile.";
            }
            else if (goal.Affordable)
            {
                guideEyebrow.text = "NEXT IMPROVEMENT";
                guideWhy.text = Money(goal.Cost) + " coins · " + (goal.WaitsForBuilder ? "when the builder is free in " + TimeLabel(goal.BuilderFreeInSeconds) : "ready to buy");
            }
            else
            {
                guideEyebrow.text = "SAVING FOR";
                var eta = goal.SecondsToAfford(coinsPerSecond);
                guideWhy.text = Money(Math.Min(goal.Cost, goal.Wallet + goal.Tills)) + " of " + Money(goal.Cost) + " coins"
                    + (eta > 0 ? " · about " + TimeLabel(eta) : "") + (goal.Tills > 0 ? " · collect your counters" : "");
            }
            // During the doctors clinic lesson every starter improvement is a step towards it.
            if (towardsLesson) guideEyebrow.text = "DOCTORS CLINIC · " + guideEyebrow.text;
        }

        /// <summary>Take the player to where the next goal happens.</summary>
        private void GoToNextGoal()
        {
            var goal = nextGoal;
            if (goal == null) return;
            if (goal.Option != null && goal.Option.Kind == ClinicPurchaseKind.Amenity) { SelectObject(AmenityHit(goal.Option.Amenity)); return; }
            if (goal.Room.HasValue) { Select(goal.Room.Value); return; }
            SelectObject(new ClinicHit(ClinicHitKind.BossOffice));
        }

        private void ShowGuideSkip(bool show)
        {
            if (guideSkip == null) return;
            var display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (guideSkip.style.display == display) return;
            guideSkip.style.display = display; ApplySafeArea();
        }

        /// <summary>Skip the current optional lesson. Nothing is spent or earned; it stays listed under Goals.</summary>
        private void OnGuideSkip()
        {
            var step = CurrentGuide();
            if (step == null || !ClinicGuide.CanSkip(step) || saves.IsGuideStepDone(step)) return;
            if (!saves.SkipGuideStep(step.Id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That lesson could not be skipped.", 6); return; }
            RebindAfterCommit();
            var next = saves.CurrentGuideStep();
            Notify("Lesson skipped. It stays under Gems & goals › Goals." + (next == null ? "" : " Next: " + next.Title), 5);
        }

        /// <summary>Per frame: a small arrow floats above where the next step happens.</summary>
        private void UpdateGuidePointer()
        {
            if (guidePointer == null) return;
            var step = CurrentGuide();
            var show = step != null && dock.style.display != DisplayStyle.Flex && !saves.IsGuideStepDone(step) && step.Location == State.Location
                && (step.Focus == ClinicGuideFocus.Room || step.Focus == ClinicGuideFocus.Amenity);
            if (!show) { guidePointer.style.display = DisplayStyle.None; return; }
            var point = step.Focus == ClinicGuideFocus.Room ? world.GetRoomPoint(step.Room) : world.GetAmenityPoint(step.Amenity);
            var bob = ReducedMotion ? 0 : Mathf.Sin(Time.unscaledTime * 4f) * 5f;
            PositionMarker(guidePointer, world.WorldToViewport(point), true, -44 + bob, new Vector2(40, 40));
        }

        private void OnGuideTap()
        {
            if(State.OpeningEnabled && State.OpeningStep!=ClinicOpeningStep.Complete){OpenOpeningTask();return;}
            var step = CurrentGuide();
            if (nextGoal != null && ShowsNextGoal(step)) { GoToNextGoal(); return; }
            if (step == null) return;
            if (saves.IsGuideStepDone(step))
            {
                var from = guideCard.worldBound.center;
                if (!saves.ClaimGuideStep(step.Id, DateTimeOffset.UtcNow)) { Notify(saves.Error ?? "That reward could not be collected.", 6); return; }
                RebindAfterCommit();
                LaunchGemsFrom(overlay.WorldToLocal(from), (int)Math.Min(8, step.GemReward));
                Feedback(1);
                clinicAudio.PlayReward();
                var next = saves.CurrentGuideStep();
                Notify("+" + step.GemReward + " gems" + (next == null ? " · Guide complete!" : " · Next: " + next.Title), 4);
                return;
            }
            if (step.Location != State.Location)
            {
                ToggleLocations();
                Notify("Travel to your " + (step.Location == ClinicLocation.DoctorsClinic ? "doctors clinic" : "starter clinic") + " for this step.", 5);
                return;
            }
            switch (step.Focus)
            {
                case ClinicGuideFocus.Room:
                    Select(step.Room);
                    if (selectedRoom != step.Room) Notify(step.Why, 5);
                    break;
                case ClinicGuideFocus.Amenity: SelectObject(AmenityHit(step.Amenity)); break;
                case ClinicGuideFocus.Gems: ToggleGems(); break;
                case ClinicGuideFocus.Locations: ToggleLocations(); break;
            }
        }
    }
}
