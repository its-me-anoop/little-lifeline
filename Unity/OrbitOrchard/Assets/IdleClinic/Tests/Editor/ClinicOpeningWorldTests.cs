using System.Linq;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicOpeningWorldTests
    {
        [Test]
        public void BossTravelsToTheNursingRoomAndOpensItsDoor()
        {
            var host=new GameObject("Boss journey test");
            try
            {
                var sim=ClinicSimulation.CreateOpening();
                void Finish(ClinicOpeningStep step)
                {
                    Assert.That(sim.ContinueOpening(step).Success,Is.True,step.ToString());
                    for(int guard=0;guard<400&&sim.State.OpeningStep==step;guard++)sim.Advance(.1);
                }
                foreach(var step in new[]{ClinicOpeningStep.CleanOffice,ClinicOpeningStep.SetUpOffice,ClinicOpeningStep.CleanReception,ClinicOpeningStep.SetUpReception})Finish(step);
                // The boss checks in the first visitor; then the waiting room is next.
                for(int guard=0;guard<900&&sim.State.OpeningStep!=ClinicOpeningStep.CleanWaiting;guard++)sim.Advance(.1);
                foreach(var step in new[]{ClinicOpeningStep.CleanWaiting,ClinicOpeningStep.SetUpWaiting})Finish(step);
                sim.Advance(4);
                Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.CleanNursing));
                var world=host.AddComponent<ClinicWorld>();world.Render(sim.State,0,true);
                var boss=host.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Clinic boss");
                var before=boss.position;var previous=before;int doors=world.DoorOpeningCount;float furthest=0,fastest=0;
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.CleanNursing).Success,Is.True);
                // Render every tenth of a second from the start of the task, as play does, until the room is clean.
                for(int frame=0;frame<400&&sim.State.OpeningStep==ClinicOpeningStep.CleanNursing;frame++)
                {
                    sim.Advance(.1);world.Render(sim.State,.1f,true);
                    furthest=Mathf.Max(furthest,Vector3.Distance(before,boss.position));
                    fastest=Mathf.Max(fastest,Vector3.Distance(previous,boss.position)/.1f);previous=boss.position;
                }
                Assert.That(furthest,Is.GreaterThan(1),"The boss walks between rooms.");
                Assert.That(world.DoorOpeningCount,Is.GreaterThan(doors),"The boss opens the nursing door along the journey.");
                Assert.That(fastest,Is.LessThanOrEqualTo((float)ClinicBossRoute.MetresPerSecond+.05f),"The boss walks at a steady pace and never jumps.");
                Assert.That(Vector3.Distance(boss.position,BossPoint(1)),Is.LessThan(.05f),"The boss ends in the nursing room.");
            }
            finally{Object.DestroyImmediate(host);}
        }

        private static Vector3 BossPoint(int zone){var p=ClinicBossRoute.Point(zone);return new Vector3((float)p.X,.14f,(float)p.Z);}

        [Test]
        public void FreshClinicShowsBossAndClutterInsteadOfFurniture()
        {
            var host=new GameObject("Opening world test");
            try
            {
                var world=host.AddComponent<ClinicWorld>();var sim=ClinicSimulation.CreateOpening();world.Render(sim.State,0,true);
                var nodes=host.GetComponentsInChildren<Transform>(true);
                Assert.That(nodes.Single(t=>t.name=="Clinic boss").gameObject.activeInHierarchy,Is.True);
                Assert.That(nodes.Any(t=>t.name=="Empty cardboard box"&&t.gameObject.activeInHierarchy),Is.True);
                foreach(var name in new[]{"ReceptionDesk","TreatmentBay","Seat","Office level one furniture","Cleaning utilities furniture"})
                    Assert.That(nodes.Where(t=>t.name==name).All(t=>!t.gameObject.activeInHierarchy),Is.True,name+" starts empty.");
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.CleanOffice).Success,Is.True);sim.Advance(8);world.Render(sim.State,0,true);
                Assert.That(nodes.Single(t=>t.name=="Opening clutter 3").gameObject.activeSelf,Is.False);
                Assert.That(nodes.Single(t=>t.name=="Office level one furniture").gameObject.activeSelf,Is.False);
                sim.ContinueOpening(ClinicOpeningStep.SetUpOffice);sim.Advance(10);world.Render(sim.State,0,true);
                Assert.That(nodes.Single(t=>t.name=="Office level one furniture").gameObject.activeSelf,Is.True);
                Assert.That(nodes.Single(t=>t.name=="Opening clutter 0").gameObject.activeSelf,Is.True);
            }
            finally{Object.DestroyImmediate(host);}
        }
    }
}
