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
                foreach(var step in new[]{ClinicOpeningStep.CleanOffice,ClinicOpeningStep.SetUpOffice,ClinicOpeningStep.CleanReception,ClinicOpeningStep.SetUpReception})
                {sim.ContinueOpening(step);sim.Advance(10);}
                sim.Advance(30);
                foreach(var step in new[]{ClinicOpeningStep.CleanWaiting,ClinicOpeningStep.SetUpWaiting})
                {sim.ContinueOpening(step);sim.Advance(10);}
                sim.Advance(4);
                var world=host.AddComponent<ClinicWorld>();world.Render(sim.State,0,true);
                var boss=host.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Clinic boss");
                var before=boss.position;int doors=world.DoorOpeningCount;
                sim.ContinueOpening(ClinicOpeningStep.CleanNursing);
                sim.Advance(3);world.Render(sim.State,.1f,true);
                Assert.That(Vector3.Distance(before,boss.position),Is.GreaterThan(1),"The boss walks between rooms.");
                for(int tick=0;tick<50;tick++){sim.Advance(.1);world.Render(sim.State,.1f,true);}
                Assert.That(world.DoorOpeningCount,Is.GreaterThan(doors),"The boss opens the nursing door along the journey.");
            }
            finally{Object.DestroyImmediate(host);}
        }

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
