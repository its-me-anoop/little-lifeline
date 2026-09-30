#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.App
{
    /// <summary>Development-build QA switches for headless simulator checks. Compiled out of release builds.
    /// Example: SIMCTL_CHILD_CLINIC_QA_CLAIM_ALL=1 SIMCTL_CHILD_CLINIC_QA_RENOVATE=FirstAid
    ///          SIMCTL_CHILD_CLINIC_QA_OPEN=gems:shop xcrun simctl launch booted com.flutterly.gravitile</summary>
    public sealed partial class ClinicApp
    {
        private void ApplyQALaunchArguments()
        {
            var args = Environment.GetCommandLineArgs();
            string Value(string name, string flag)
            {
                var environment = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrEmpty(environment)) return environment;
                var index = Array.IndexOf(args, flag);
                return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
            }
            var claimAll = Value("CLINIC_QA_CLAIM_ALL", "-qaClaimAll") != null || Array.IndexOf(args, "-qaClaimAll") >= 0;
            var renovate = Value("CLINIC_QA_RENOVATE", "-qaRenovate");
            var open = Value("CLINIC_QA_OPEN", "-qaOpen");
            var skip = Value("CLINIC_QA_SKIP", "-qaSkip") != null;
            var guide = Value("CLINIC_QA_GUIDE", "-qaGuide") != null;
            // Camera framing for store captures: "factor,u,v", for example "0.7,0.5,0.6".
            var zoom = Value("CLINIC_QA_ZOOM", "-qaZoom");
            // Room style preview: "tier" or "tier,decor", for example "12,6".
            var style = Value("CLINIC_QA_STYLE", "-qaStyle");
            if (style != null)
            {
                var parts = style.Split(',');
                if (int.TryParse(parts[0], out var tier)) IdleClinic.Presentation.ClinicWorld.PreviewTier = tier;
                if (parts.Length > 1 && int.TryParse(parts[1], out var decor)) IdleClinic.Presentation.ClinicWorld.PreviewDecor = decor;
            }
            // First aid equipment preview: every item at one version, for example "7".
            var gear = Value("CLINIC_QA_GEAR", "-qaGear");
            if (gear != null && int.TryParse(gear, out var gearVersion)) IdleClinic.Presentation.ClinicWorld.PreviewGear = gearVersion;
            // Screenshot state: play the opening (collect, hire), earn extra coins, then let the clinic run for a while.
            if (Value("CLINIC_QA_TUTORIAL", "-qaTutorial") != null && State.Tutorial != ClinicTutorialStep.Complete)
            { simulation.Advance(25); simulation.Collect(0); simulation.HireNurse(); simulation.Advance(30); }
            if (long.TryParse(Value("CLINIC_QA_COINS", "-qaCoins"), out var coins)) ClinicSimulation.TryGrantReward(State, coins);
            // Room size and equipment progress for a mid-game look: every built room to this size, then this many upgrades each.
            if (int.TryParse(Value("CLINIC_QA_TIER", "-qaTier"), out var roomSize))
            {
                if (State.Tutorial == ClinicTutorialStep.Complete && State.Location == ClinicLocation.StarterClinic) { State.WaitingRoomUnlocked = true; State.Room(ClinicRoom.Waiting).Built = true; }
                foreach (var built in State.Rooms) if (built.Built) built.Tier = Math.Min(roomSize, ClinicRules.MaximumTier(State));
            }
            if (int.TryParse(Value("CLINIC_QA_UPGRADES", "-qaUpgrades"), out var upgrades))
                foreach (var built in State.Rooms.ToArray())
                    for (var k = 0; k < upgrades && built.Built; k++)
                    {
                        var kind = built.Kind; var item = -1; var pieces = ClinicGear.UnlockedCount(State, kind);
                        for (var o = 0; o < pieces && item < 0; o++) { var i = (k + o) % pieces; if (!ClinicGear.AtTop(State, kind, i)) item = i; }
                        if (item < 0) break;
                        ClinicSimulation.TryGrantReward(State, ClinicGear.UpgradeCost(State, kind, item));
                        simulation.UpgradeGear(kind, item);
                    }
            if (double.TryParse(Value("CLINIC_QA_ADVANCE", "-qaAdvance"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds)) simulation.Advance(seconds, false);
            if (!claimAll && renovate == null && open == null && !skip && !guide && zoom == null) return;
            if (zoom != null)
            {
                var parts = zoom.Split(',');
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                if (parts.Length == 3 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out var factor)
                    && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out var u)
                    && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, culture, out var v))
                    world.Zoom(factor, new Vector2(u, v));
            }
            if (guide)
                for (var step = saves.CurrentGuideStep(); step != null && saves.IsGuideStepDone(step); step = saves.CurrentGuideStep())
                    if (saves.ClaimGuideStep(step.Id, DateTimeOffset.UtcNow)) RebindAfterCommit(); else break;
            Debug.Log("Clinic QA: claimAll=" + claimAll + " renovate=" + renovate + " open=" + open + " skip=" + skip);
            if (claimAll)
                foreach (var goal in saves.ClaimableMilestones())
                    if (saves.ClaimMilestone(goal.Id, DateTimeOffset.UtcNow)) RebindAfterCommit();
            if (Enum.TryParse<ClinicRoom>(renovate, out var room))
            {
                Run(() => simulation.Renovate(room));
                Select(room);
            }
            if (skip && State.Construction.Count > 0) SkipConstruction(State.Construction[0].Id);
            if (open != null && open.StartsWith("gems"))
            {
                // gems, gems:goals or gems:shop
                if (Enum.TryParse<GemTab>(open.Substring(Math.Min(open.Length, 5)), true, out var tab)) gemTab = tab;
                ToggleGems();
            }
            else if (open == "settings") ToggleSettings();
            else if (Enum.TryParse<ClinicRoom>(open, out var selected)) StartCoroutine(SelectAfterFirstFrames(selected));
        }

        // The room panel needs the world's first frames (its picture is rendered from the scene), so open it a moment after launch.
        private System.Collections.IEnumerator SelectAfterFirstFrames(ClinicRoom room)
        {
            yield return new WaitForSeconds(1.5f);
            Select(room);
        }
    }
}
#endif
