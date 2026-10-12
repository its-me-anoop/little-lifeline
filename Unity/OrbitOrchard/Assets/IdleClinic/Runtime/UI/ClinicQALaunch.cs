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
            // Video captures: the doctors clinic, a fully staffed clinic, and a clean frame without any interface.
            // The doctors clinic: plays the opening if needed, completes the starter clinic through its real rules (QA coins,
            // building time skipped) and opens the doctors clinic as a player would. Later switches then apply to it.
            if (Value("CLINIC_QA_DOCTORS", "-qaDoctors") != null && State.Location == ClinicLocation.StarterClinic)
            {
                if (State.Tutorial != ClinicTutorialStep.Complete) PlayOpeningTo(ClinicOpeningStep.Complete);
                if (saves.QaOpenDoctorsClinic(DateTimeOffset.UtcNow)) PublishLocation();
                else Debug.LogWarning("Clinic QA: doctors clinic not opened: " + saves.Error);
            }
            if (Value("CLINIC_QA_CLEAN", "-qaClean") != null) StartCoroutine(HideInterface());
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
            if (Value("CLINIC_QA_FULL", "-qaFull") != null)
                for (var pass = 0; pass < 12; pass++)
                {
                    foreach (ClinicStaffRole role in Enum.GetValues(typeof(ClinicStaffRole)))
                    {
                        ClinicSimulation.TryGrantReward(State, 10000000); simulation.AddStation(role);
                        ClinicSimulation.TryGrantReward(State, 10000000); simulation.HireStaff(role);
                    }
                    foreach (ClinicAmenity kind in Enum.GetValues(typeof(ClinicAmenity)))
                    { ClinicSimulation.TryGrantReward(State, 10000000); simulation.UpgradeAmenity(kind); }
                }
            if (double.TryParse(Value("CLINIC_QA_ADVANCE", "-qaAdvance"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var seconds)) simulation.Advance(seconds, false);
            // The opening story: "name" (the name question, panel open), "reply" (where to start, panel open) or "skip".
            // CLINIC_QA_STORY_NAME answers the name question with that name.
            var story = Value("CLINIC_QA_STORY", "-qaStory");
            var storyName = Value("CLINIC_QA_STORY_NAME", "-qaStoryName");
            if (story != null || storyName != null) PlayStoryTo(story, storyName);
            // 4.3 (23) captures. Opening: play the boss opening up to a step ("FirstCheckIn", "HireNurse", "Complete") as taps would.
            var opening = Value("CLINIC_QA_OPENING", "-qaOpening");
            if (opening != null && Enum.TryParse<ClinicOpeningStep>(opening, true, out var openingTarget)) PlayOpeningTo(openingTarget);
            // A progressed clinic earned through play: this many hours of collecting and buying the cheapest improvement.
            if (double.TryParse(Value("CLINIC_QA_PLAY", "-qaPlay"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var playHours) && playHours > 0)
                PlayFor(playHours);
            // Gems for an isolated QA save, so a confirmation can be shown with enough or too few gems.
            if (long.TryParse(Value("CLINIC_QA_GEMS", "-qaGems"), out var qaGems) && qaGems > 0 && saves.QaGrantGems(qaGems, DateTimeOffset.UtcNow)) RebindAfterCommit();
            // Time away: apply this many seconds of offline progress and show the welcome-back report.
            if (double.TryParse(Value("CLINIC_QA_AWAY", "-qaAway"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var away) && away > 0)
            {
                SaveNow();
                var report = saves.ApplyOffline(DateTimeOffset.UtcNow.AddSeconds(away));
                profile = saves.Profile; BindSimulations();
                if (report.applied) ShowWelcome(report);
            }
            // A confirmation sheet, for example "Boost:boost.1h", "CoinPack:coins.bag", "Decoration:Reception" or "FinishWork:first".
            var confirm = Value("CLINIC_QA_CONFIRM", "-qaConfirm");
            if (confirm != null) StartCoroutine(OpenConfirmationAfterFirstFrames(confirm));
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
                if (open == "gems:extras") { gemTab = GemTab.Shop; shopExtrasOpen = true; }
                else if (Enum.TryParse<GemTab>(open.Substring(Math.Min(open.Length, 5)), true, out var tab)) gemTab = tab;
                ToggleGems();
            }
            else if (open == "settings") ToggleSettings();
            else if (open == "settings:story") { ToggleSettings(); OpenStoryPage(); }
            else if (open == "opening") StartCoroutine(OpenOpeningAfterFirstFrames());
            else if (open == "BossOffice" || open == "CleaningUtilities")
                StartCoroutine(SelectSupportRoomAfterFirstFrames(open == "BossOffice"));
            else if (Enum.TryParse<ClinicRoom>(open, out var selected)) StartCoroutine(SelectAfterFirstFrames(selected));
        }

        /// <summary>Play the opening as a player would: start each task, let it finish, collect the first payment and hire the
        /// included staff, stopping at <paramref name="target"/>.</summary>
        private void PlayOpeningTo(ClinicOpeningStep target)
        {
            if (!State.OpeningEnabled) return;
            FinishStoryPrologue();
            for (var guard = 0; guard < 400 && State.OpeningStep < target && State.OpeningStep != ClinicOpeningStep.Complete; guard++)
            {
                var step = State.OpeningStep;
                if (State.OpeningTaskEndsTick < 0 && step != ClinicOpeningStep.FirstCheckIn && step != ClinicOpeningStep.QueueArrivals)
                    simulation.ContinueOpening(step);
                simulation.Advance(1, false);
                foreach (var desk in State.ReceptionDesks) if (desk.Till > 0) simulation.Collect(desk.Id);
            }
            simulation.DrainEvents();
            SaveNow();
            dockKey = ""; UpdateReadouts();
        }

        /// <summary>Read Maya's prologue as a player would, stopping at the name question ("name"), at where to start
        /// ("reply"), or skipping the story ("skip").</summary>
        private void PlayStoryTo(string target, string name)
        {
            var script = StoryScript; var told = Story;
            if (told == null || !told.enabled) return;
            for (var guard = 0; guard < 16; guard++)
            {
                var beat = ClinicStory.Current(script, told, State);
                if (beat == null || beat.Kind != ClinicStoryBeatKind.Prologue) break;
                if (beat.AsksName) { if (target == "name") break; ClinicStory.Answer(script, told, State, name != null ? 0 : 1, name); continue; }
                if (beat.Replies.Count > 0 || !ClinicStory.Continue(script, told, State)) break;
            }
            if (target == "skip") ClinicStory.Skip(told, State);
            SaveNow(); UpdateStory(); dockKey = ""; UpdateReadouts();
            if (target == "name" || target == "reply") StartCoroutine(OpenOpeningAfterFirstFrames());
        }

        /// <summary>Answer the rest of the prologue with the first answers (keeping any name already chosen), as taps would.</summary>
        private void FinishStoryPrologue()
        {
            var script = StoryScript; var told = Story;
            if (told == null || !told.enabled) return;
            for (var guard = 0; guard < 16; guard++)
            {
                var beat = ClinicStory.Current(script, told, State);
                if (beat == null || beat.Kind != ClinicStoryBeatKind.Prologue) break;
                var moved = beat.AsksName ? ClinicStory.Answer(script, told, State, told.bossName.Length > 0 ? 0 : 1, told.bossName)
                    : beat.Replies.Count > 0 ? ClinicStory.Answer(script, told, State, 0) : ClinicStory.Continue(script, told, State);
                if (!moved) break;
            }
            UpdateStory();
        }

        private System.Collections.IEnumerator OpenOpeningAfterFirstFrames()
        {
            yield return new WaitForSeconds(1.5f);
            if (State.OpeningEnabled && State.OpeningStep != ClinicOpeningStep.Complete) OpenOpeningTask();
        }

        private void PlayFor(double hours)
        {
            if (State.Tutorial != ClinicTutorialStep.Complete) PlayOpeningTo(ClinicOpeningStep.Complete);
            var end = State.Tick + (long)(Math.Min(hours, 200) * 3600 * ClinicRules.TicksPerSecond);
            for (var guard = 0; guard < 200000 && State.Tick < end; guard++)
            {
                simulation.Advance(30, false);
                foreach (var desk in State.ReceptionDesks) if (desk.Till > 0) simulation.Collect(desk.Id);
                simulation.CollectVendingTips(); simulation.CollectParkingFees();
                for (var buys = 0; buys < 20; buys++)
                {
                    ClinicPurchaseOption cheapest = null;
                    foreach (var option in ClinicPurchases.Options(simulation)) if (cheapest == null || option.Cost < cheapest.Cost) cheapest = option;
                    if (cheapest == null || cheapest.Cost > State.Wallet || !ClinicPurchases.Buy(simulation, cheapest).Success) break;
                }
            }
            simulation.DrainEvents();
            SaveNow();
            dockKey = ""; UpdateReadouts();
        }

        private System.Collections.IEnumerator OpenConfirmationAfterFirstFrames(string confirm)
        {
            yield return new WaitForSeconds(1.5f);
            var parts = confirm.Split(new[] { ':' }, 2);
            if (parts.Length != 2 || !Enum.TryParse<IdleClinic.Services.ClinicGemSpendKind>(parts[0], true, out var kind)) yield break;
            var key = parts[1];
            if (kind == IdleClinic.Services.ClinicGemSpendKind.FinishWork && key == "first")
            {
                if (State.Construction.Count == 0) yield break;
                lastFinishRequestRoom = State.Construction[0].Room;
                key = State.Construction[0].Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            RequestGemSpend(kind, key);
        }

        private System.Collections.IEnumerator SelectSupportRoomAfterFirstFrames(bool office)
        {
            yield return new WaitForSeconds(1.5f);
            SelectObject(new IdleClinic.Presentation.ClinicHit(office?IdleClinic.Presentation.ClinicHitKind.BossOffice:IdleClinic.Presentation.ClinicHitKind.CleaningUtilities));
        }

        private System.Collections.IEnumerator HideInterface()
        {
            while (true)
            {
                foreach (var element in new[] { header, hint, cameraTools, hudChips, overlay, dock, scrim, guideCard, guideSkip, gemConfirm, welcomeCard, banner })
                    if (element != null) element.style.display = UnityEngine.UIElements.DisplayStyle.None;
                yield return null;
            }
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
