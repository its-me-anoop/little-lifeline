using IdleClinic.Core;
using IdleClinic.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    public sealed partial class ClinicApp
    {
        // The player is the boss: every opening step speaks to them directly.
        private static readonly string[] OpeningTitles = {
            "Clean your office", "Set up your office · level 1", "Clean reception", "Set up the reception desk",
            "Check in your first patient", "Clean the waiting area", "Set up the waiting area · level 1",
            "Clean the nursing room", "Set up the nursing station", "Hire your first nurse",
            "Patients are arriving", "Recruit a receptionist", "Your clinic is open" };
        private static readonly string[] OpeningReasons = {
            "Clear the empty boxes and litter.", "A desk and computer to run the clinic from.",
            "Clear the entrance before welcoming anyone.", "A level 1 desk. You’ll cover reception yourself for now.",
            "Your first patient is at the desk.", "Your first patient needs somewhere comfortable to wait.",
            "The first seats, facing the nursing rooms.", "Prepare a clean space for patient care.",
            "The first level 1 nursing station.", "Your first nurse is included in the opening kit.",
            "The nursing room is ready. Visitors can join the queue.", "Someone to take over the desk from you. Included in your opening kit.",
            "The team can now look after the clinic." };

        private void OpenOpeningTask()
        {
            settingsOpen=false;gemsOpen=false;locationsOpen=false;
            if (ClinicSimulation.OpeningZone(State.OpeningStep)==3)
            { selectedRoom=null;selectedObject=new ClinicHit(ClinicHitKind.BossOffice);world.SelectRoom(selectedObject.Value); }
            else
            { selectedObject=null;selectedRoom=ClinicSimulation.OpeningRoom(State.OpeningStep);world.SelectRoom(selectedRoom.Value); }
            dockKey="";UpdateReadouts();
        }
        private void PitchIn()
        {
            var room=State.OpeningStep;
            if(!simulation.HelpOpeningTask().Success)return;
            Feedback(0);
            StoryPitchedIn();
            var point=ClinicSimulation.OpeningZone(room)==3?world.GetSupportRoomPoint(true):world.GetRoomPoint(ClinicSimulation.OpeningRoom(room));
            FloatText(point,ClinicSimulation.OpeningCleaning(room)?"Helping!":"Nearly there!","float-good");
            UpdateReadouts();
        }

        private void UpdateOpeningGuide()
        {
            var step=State.OpeningStep;bool busy=State.OpeningTaskEndsTick>=0;
            var display=dock.style.display==DisplayStyle.Flex?DisplayStyle.None:DisplayStyle.Flex;
            if(guideCard.style.display!=display){guideCard.style.display=display;ApplySafeArea();}
            guideRewardGem.parent.style.display=DisplayStyle.None;
            guidePointer.style.display=DisplayStyle.None;
            guideCard.EnableInClassList("guide-ready",false);
            // Maya does the talking while the story is told; otherwise the plain task.
            if(UpdateStoryCard(busy))return;
            // Progress through the twelve opening steps, so a returning player sees how far the clinic has come.
            guideEyebrow.text=(busy?"AT WORK":"OPEN YOUR CLINIC")+" · "+((int)step+1)+" OF "+(int)ClinicOpeningStep.Complete;
            guideTitle.text=OpeningTitles[(int)step];
            guideWhy.text=busy?"Ready in "+TimeLabel((State.OpeningTaskEndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond)+" · tap to pitch in":OpeningReasons[(int)step];
        }
        private void BuildOpeningDock()
        {
            var step=State.OpeningStep;
            var beat=storyBeat;
            if(beat!=null)BuildStoryQuote(dock,beat);
            if(beat!=null&&beat.Kind==ClinicStoryBeatKind.Prologue){BuildPrologueDock(beat);return;}
            Text(dock,OpeningTitles[(int)step],"track-title",true);
            Text(dock,OpeningReasons[(int)step],"room-detail");
            if(State.OpeningTaskEndsTick>=0)
            {
                var status=Text(dock,"","room-detail");
                readouts.Add(()=>status.text="At work · "+TimeLabel((State.OpeningTaskEndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond)+" remaining");
                // Optional: lend a hand instead of watching the timer. Each tap takes a moment off; waiting works just as well.
                var help=IconButton(dock,ClinicGlyph.Bolt,"Pitch in, finishes a little sooner",PitchIn,"track-action help-action");
                help.name="opening-help";
                Text(help,"Pitch in","action-label",true);
                return;
            }
            if(step==ClinicOpeningStep.FirstCheckIn || step==ClinicOpeningStep.QueueArrivals) return;
            // Hiring Maya and the receptionist: the answers are the actions.
            if(beat!=null&&beat.Replies.Count>0){BuildStepAnswers(beat);return;}
            // The whole task as the player will see it: the walk from where they stand, then the work.
            var seconds=(int)System.Math.Ceiling(ClinicSimulation.OpeningTaskTicks(step,State.BossZone)/(double)ClinicRules.TicksPerSecond);
            var label=ClinicSimulation.OpeningCleaning(step)?"Clean · "+seconds+"s":ClinicSimulation.OpeningManual(step)?"Set up · "+seconds+"s"
                :step==ClinicOpeningStep.HireNurse?"Hire first nurse":"Recruit receptionist";
            var captured=step;
            var button=IconButton(dock,ClinicSimulation.OpeningCleaning(step)?ClinicGlyph.Facility:ClinicGlyph.Upgrade,label,
                ()=>Run(()=>simulation.ContinueOpening(captured),true),"track-action primary-action");
            button.name="opening-action";
            Text(button,label,"action-label",true);
        }
    }
}
