using IdleClinic.Core;
using IdleClinic.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    public sealed partial class ClinicApp
    {
        private static readonly string[] OpeningTitles = {
            "Clean the boss office", "Set up the office · level 1", "Clean reception", "Set up the first reception desk",
            "The boss checks in the first patient", "Clean the waiting area", "Set up the waiting area · level 1",
            "Clean the first nursing room", "Set up the first nursing station", "Hire your first nurse",
            "Patients are arriving", "Recruit your first receptionist", "Your clinic is open" };
        private static readonly string[] OpeningReasons = {
            "Help the boss clear the empty boxes and litter.", "Give the boss a desk and computer to run the clinic.",
            "Clear the entrance before welcoming anyone.", "Install a level 1 desk. The boss will cover reception.",
            "Watch the boss welcome your first patient.", "Your first patient needs somewhere comfortable to wait.",
            "Add the first seats, facing the nursing rooms.", "Prepare a clean space for patient care.",
            "Install the first level 1 nursing station.", "Your first nurse is included in the opening kit.",
            "The nursing room is ready. Visitors can join the queue.", "Let a receptionist take over from the boss. Included in your opening kit.",
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
        private void HelpBoss()
        {
            var room=State.OpeningStep;
            if(!simulation.HelpOpeningTask().Success)return;
            Feedback(0);
            var point=ClinicSimulation.OpeningZone(room)==3?world.GetSupportRoomPoint(true):world.GetRoomPoint(ClinicSimulation.OpeningRoom(room));
            FloatText(point,ClinicSimulation.OpeningCleaning(room)?"Helping!":"Nearly there!","float-good");
            UpdateReadouts();
        }

        private void UpdateOpeningGuide()
        {
            var step=State.OpeningStep;bool busy=State.OpeningTaskEndsTick>=0;
            var display=dock.style.display==DisplayStyle.Flex?DisplayStyle.None:DisplayStyle.Flex;
            if(guideCard.style.display!=display){guideCard.style.display=display;ApplySafeArea();}
            // Progress through the twelve opening steps, so a returning player sees how far the clinic has come.
            guideEyebrow.text=(busy?"BOSS AT WORK":"OPEN YOUR CLINIC")+" · "+((int)step+1)+" OF "+(int)ClinicOpeningStep.Complete;
            guideTitle.text=OpeningTitles[(int)step];
            guideWhy.text=busy?"Ready in "+TimeLabel((State.OpeningTaskEndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond)+" · tap to help":OpeningReasons[(int)step];
            guideRewardGem.parent.style.display=DisplayStyle.None;
            guidePointer.style.display=DisplayStyle.None;
            guideCard.EnableInClassList("guide-ready",false);
        }
        private void BuildOpeningDock()
        {
            var step=State.OpeningStep;
            Text(dock,OpeningTitles[(int)step],"track-title",true);
            Text(dock,OpeningReasons[(int)step],"room-detail");
            if(State.OpeningTaskEndsTick>=0)
            {
                var status=Text(dock,"","room-detail");
                readouts.Add(()=>status.text="Boss at work · "+TimeLabel((State.OpeningTaskEndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond)+" remaining");
                // Optional: lend a hand instead of watching the timer. Each tap takes a moment off; waiting works just as well.
                var help=IconButton(dock,ClinicGlyph.Bolt,"Help the boss, finishes a little sooner",HelpBoss,"track-action help-action");
                help.name="opening-help";
                Text(help,ClinicSimulation.OpeningCleaning(step)?"Help clear up":"Help set up","action-label",true);
                return;
            }
            if(step==ClinicOpeningStep.FirstCheckIn || step==ClinicOpeningStep.QueueArrivals) return;
            var seconds=ClinicSimulation.OpeningTaskTicks(step)/ClinicRules.TicksPerSecond;
            var label=ClinicSimulation.OpeningCleaning(step)?"Clean · "+seconds+"s":ClinicSimulation.OpeningManual(step)?"Set up · "+seconds+"s"
                :step==ClinicOpeningStep.HireNurse?"Hire first nurse":"Recruit first receptionist";
            var captured=step;
            var button=IconButton(dock,ClinicSimulation.OpeningCleaning(step)?ClinicGlyph.Facility:ClinicGlyph.Upgrade,label,
                ()=>Run(()=>simulation.ContinueOpening(captured),true),"track-action primary-action");
            button.name="opening-action";
            Text(button,label,"action-label",true);
        }
    }
}
