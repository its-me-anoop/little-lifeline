using System.Linq;
using System.Collections.Generic;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicEmojiTests
    {
        private GameObject host;private ClinicWorld world;
        [SetUp] public void Setup(){host=new GameObject("Emoji test");world=host.AddComponent<ClinicWorld>();world.Initialize();}
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(host);
        private Transform[] Bubbles()=>host.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Patient emoji bubble")&&t.gameObject.activeInHierarchy).ToArray();
        private ClinicState Queue()
        {
            var state=ClinicSimulation.CreateNew().State;state.Patients.Clear();state.Staff.Clear();
            for(int i=0;i<5;i++)state.Patients.Add(new ClinicPatientState{Id=20+i,Phase=ClinicPatientPhase.ReceptionQueue,FromAnchor=ClinicRules.QueueAnchor(i),ToAnchor=ClinicRules.QueueAnchor(i)});
            return state;
        }
        [Test] public void RandomEmojiAreStaggeredCameraFacingAndLimitedToTwo()
        {
            var state=Queue();var expressions=new HashSet<string>();bool hidden=false;
            for(int seconds=0;seconds<120;seconds++)
            {
                state.Tick=seconds*10;world.Render(state,.1f,true);var bubbles=Bubbles();
                Assert.That(bubbles.Length,Is.LessThanOrEqualTo(2));hidden|=bubbles.Length==0;
                foreach(var bubble in bubbles)
                {expressions.Add(bubble.name);Assert.That(Vector3.Dot(bubble.forward,world.SceneCamera.transform.forward),Is.GreaterThan(.999f));Assert.That(bubble.localScale,Is.EqualTo(Vector3.one));}
            }
            Assert.That(expressions.Count,Is.GreaterThanOrEqualTo(3));Assert.That(hidden,Is.True);
        }
        [Test] public void PooledVisitorsDoNotCarryEmojiIntoVehiclesOrNewArrivals()
        {
            var state=Queue();for(int i=0;i<10;i++){state.Tick=i*10;world.Render(state,.1f,true);}
            state.Patients.Clear();world.Render(state,.1f,true);Assert.That(Bubbles(),Is.Empty);
            state.Patients.Add(new ClinicPatientState{Id=100,Phase=ClinicPatientPhase.DrivingToParking,FromAnchor="parking.bay.0.patient",ToAnchor="reception.queue.0"});
            world.Render(state,.1f,true);Assert.That(Bubbles(),Is.Empty);
            state.Patients[0].Phase=ClinicPatientPhase.ReceptionQueue;state.Patients[0].FromAnchor=state.Patients[0].ToAnchor="reception.queue.0";
            world.Render(state,.1f,true);Assert.That(Bubbles(),Is.Empty);
        }
        [Test] public void RejectedVisitorApproachesThenLeavesWithAngerWithoutChangingTheQueue()
        {
            var state=Queue();state.Tick=100;world.Render(state,.1f,true);int count=state.Patients.Count;
            world.ShowTurnedAway(new ClinicEvent{Id=100,Tick=100,Kind=ClinicEventKind.PatientTurnedAway});
            bool angry=false;float closest=float.MaxValue;
            for(int tick=100;tick<160;tick++)
            {
                state.Tick=tick;world.Render(state,.1f,true);
                var actor=host.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Turned-away patient 100");
                closest=Mathf.Min(closest,Vector3.Distance(actor.position,world.GetAnchorPoint("entrance")));
                angry|=Bubbles().Any(t=>t.name.EndsWith("Angry"));Assert.That(Bubbles().Length,Is.LessThanOrEqualTo(2));
            }
            Assert.That(closest,Is.LessThan(.10f));Assert.That(angry,Is.True);Assert.That(state.Patients.Count,Is.EqualTo(count));
        }
    }
}
