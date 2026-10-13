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

        [Test]
        public void BossHoldsTheBroomWithBothHandsAndSweepsWithIt()
        {
            var host=new GameObject("Boss sweeping test");
            try
            {
                var sim=ClinicSimulation.CreateOpening();var world=host.AddComponent<ClinicWorld>();
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.CleanOffice).Success,Is.True);
                var nodes=host.GetComponentsInChildren<Transform>(true);
                world.Render(sim.State,0,false);nodes=host.GetComponentsInChildren<Transform>(true);
                var boss=nodes.Single(t=>t.name=="Clinic boss");var broom=nodes.Single(t=>t.name=="Boss cleaning broom");
                var bones=boss.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
                var hands=new[]{System.Array.Find(bones,b=>b.name=="hand.L"),System.Array.Find(bones,b=>b.name=="hand.R")};
                var lateUpdate=typeof(ClinicWorld).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                float nearest=0,minimum=float.MaxValue,maximum=float.MinValue;int sweeping=0;
                for(int frame=0;frame<400&&sim.State.OpeningTaskEndsTick>=0;frame++)
                {
                    sim.Advance(.1);world.Render(sim.State,.1f,false);
                    if(!broom.gameObject.activeSelf)continue;
                    lateUpdate.Invoke(world,null);sweeping++;
                    var line=broom.up;
                    foreach(var hand in hands)
                    {
                        var along=Vector3.Project(hand.position-broom.position,line);
                        nearest=Mathf.Max(nearest,Vector3.Distance(hand.position,broom.position+along));
                    }
                    minimum=Mathf.Min(minimum,broom.localPosition.z);maximum=Mathf.Max(maximum,broom.localPosition.z);
                }
                Assert.That(sweeping,Is.GreaterThan(10),"The boss is seen cleaning.");
                Assert.That(nearest,Is.LessThan(.10f),"Both hands are on the handle, not hanging at his sides.");
                Assert.That(maximum-minimum,Is.GreaterThan(.2f),"The broom sweeps to and fro.");
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
