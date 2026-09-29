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
        }

        private ClinicGuideStep CurrentGuide()
            => saves == null || State.Tutorial != ClinicTutorialStep.Complete ? null : saves.CurrentGuideStep();

        private string GuideAccessibleValue()
        {
            var step = CurrentGuide();
            if (step == null) return "";
            return saves.IsGuideStepDone(step) ? step.Title + " done. Collect " + step.GemReward + " gems." : step.Title + ". " + step.Why;
        }

        private void UpdateGuide()
        {
            if (guideCard == null) return;
            var step = CurrentGuide();
            var dockOpen = dock.style.display == DisplayStyle.Flex;
            var done = step != null && saves.IsGuideStepDone(step);
            var display = step == null || dockOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (guideCard.style.display != display) { guideCard.style.display = display; ApplySafeArea(); }
            if (step != null)
            {
                guideEyebrow.text = done ? "DONE" : "NEXT STEP";
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
            var step = CurrentGuide();
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
