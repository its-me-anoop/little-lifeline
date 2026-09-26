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
            else if (Enum.TryParse<ClinicRoom>(open, out var selected)) Select(selected);
        }
    }
}
#endif
