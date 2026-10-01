using System.Linq;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class ClinicArchitectureTests
    {
        private GameObject host;
        private ClinicWorld world;
        [SetUp] public void SetUp(){host=new GameObject("Joined architecture test");world=host.AddComponent<ClinicWorld>();world.Initialize();}
        [TearDown] public void TearDown()=>Object.DestroyImmediate(host);
        private Transform Find(string name)=>host.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
        private static readonly string[] InteriorDoors={"Treatment room 1 doorway","Treatment room 2 doorway","Office doorway","WC doorway","Staff room doorway","Store doorway"};

        [Test]
        public void WallRunsHaveContinuousCornersAndOnlyAuthoredOpenings()
        {
            var walls=Find("Joined clinic walls").GetComponentsInChildren<Renderer>();
            System.Action<float,float> covered=(x,z)=>Assert.That(walls.Any(r=>r.bounds.Contains(new Vector3(x,.40f,z))),Is.True,"Unframed wall gap at "+x+", "+z);
            System.Func<float,float,float,bool> clear=(v,centre,half)=>Mathf.Abs(v-centre)<half+.03f;
            // Outer walls: the storefront opens only for the entrance; the fire exit fills its own opening.
            for(float z=-5.13f;z<=5.87f;z+=.025f){covered(-5.72f,z);covered(5.72f,z);}
            for(float x=-5.72f;x<=5.72f;x+=.025f){covered(x,5.87f);if(!clear(x,0,1.045f))covered(x,-5.13f);}
            // Across the middle: the lounge's back partition, and the reception's back wall with the staff door.
            for(float x=-5.72f;x<=-1f;x+=.025f)covered(x,-.13f);
            for(float x=1f;x<=5.72f;x+=.025f)if(!clear(x,5.2f,.5f))covered(x,-.13f);
            // The corridor's walls open only at the treatment rooms, the office and the WC.
            for(float z=-.13f;z<=5.87f;z+=.025f)
            {
                if(!clear(z,1f,.7f)&&!clear(z,4f,.7f))covered(-1f,z);
                if(!clear(z,.9f,.5f)&&!clear(z,4.2f,.5f))covered(1f,z);
            }
            for(float x=-5.72f;x<=-1f;x+=.025f)covered(x,2.87f);
            for(float z=-.13f;z<=5.87f;z+=.025f)covered(3.4f,z);
            foreach(var door in InteriorDoors)Assert.That(Find(door),Is.Not.Null,door);
        }

        [TestCase("entrance","reception.queue.0")]
        [TestCase("entrance","reception.queue.4")]
        [TestCase("entrance","reception.queue.10")]
        [TestCase("reception.queue.4","reception.desk.0.patient")]
        [TestCase("reception.queue.10","reception.desk.1.patient")]
        [TestCase("reception.queue.4","reception.queue.3")]
        [TestCase("reception.queue.7","reception.queue.6")]
        [TestCase("reception.desk.0.patient","waiting.seat.0")]
        [TestCase("reception.desk.1.patient","waiting.seat.9")]
        [TestCase("waiting.seat.0","firstaid.station.1.patient")]
        [TestCase("waiting.seat.13","firstaid.station.0.patient")]
        [TestCase("firstaid.station.1.patient","exit")]
        [TestCase("waiting.seat.13","waiting.toilet.patient")]
        [TestCase("waiting.toilet.patient","waiting.seat.5")]
        [TestCase("waiting.seat.0","waiting.vending.patient")]
        [TestCase("waiting.vending.patient","waiting.seat.0")]
        public void PatientRoutesUseFramedDoorsAndClearContinuousWalls(string from,string to)
        {
            var state=ClinicSimulation.CreateNew().State;state.Patients.Clear();state.Staff.Clear();
            state.Room(ClinicRoom.Waiting).Built=true;state.Room(ClinicRoom.Waiting).FacilitiesLevel=6;
            state.Room(ClinicRoom.FirstAid).StationCount=2;
            state.Amenity(ClinicAmenity.Toilet).Level=1;state.Amenity(ClinicAmenity.Vending).Level=1;
            state.Patients.Add(new ClinicPatientState{Id=901,Phase=ClinicPatientPhase.WalkingToWaiting,FromAnchor=from,ToAnchor=to,PhaseStartedTick=0,PhaseEndsTick=150});
            world.Render(state,0,true);
            var walls=Find("Joined clinic walls").GetComponentsInChildren<Renderer>()
                .Concat(Find("Clinic entrance doorway").GetComponentsInChildren<Renderer>())
                .Concat(InteriorDoors.SelectMany(n=>Find(n).GetComponentsInChildren<Renderer>().Where(r=>r.name=="Doorway jamb"))).ToArray();
            for(int tick=0;tick<=150;tick++)
            {
                state.Tick=tick;world.Render(state,.1f);
                var body=Find("Patient 901").position+Vector3.up*.35f;
                foreach(var wall in walls)
                {
                    var bounds=wall.bounds;bounds.Expand(new Vector3(.58f,0,.58f));
                    Assert.That(bounds.Contains(body),Is.False,wall.name+" obstructs "+from+" -> "+to+" at tick "+tick+" / "+body);
                }
            }
        }

        [Test]
        public void EntranceLeavesMeetTheFrameHeadWithoutAnOpenStrip()
        {
            world.Render(ClinicSimulation.CreateNew().State,0,true);
            var root=Find("Clinic entrance doorway");
            var seal=root.GetComponentsInChildren<Transform>().First(t=>t.name=="Door head seal").GetComponent<Renderer>().bounds;
            var leaves=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Entrance panel frame").Select(t=>t.GetComponent<Renderer>().bounds).ToArray();
            Assert.That(leaves.Length,Is.EqualTo(2));
            foreach(var leaf in leaves)Assert.That(Mathf.Abs(seal.min.y-leaf.max.y),Is.LessThan(.005f));
        }

        [Test]
        public void InteriorDoorLeavesFillTheirFramesToTheCutWallHeight()
        {
            world.Render(ClinicSimulation.CreateNew().State,0,true);
            foreach(var name in InteriorDoors)
            {
                var root=Find(name);
                var jambs=root.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Doorway jamb").ToArray();
                var leaves=root.GetComponentsInChildren<Renderer>().Where(r=>r.name=="Door leaf panel").ToArray();
                Assert.That(jambs.Length,Is.EqualTo(2),name);Assert.That(leaves,Is.Not.Empty,name);
                foreach(var leaf in leaves)Assert.That(jambs[0].bounds.max.y-leaf.bounds.max.y,Is.InRange(0f,.08f),name+" leaf stops short of its frame");
            }
        }
    }
}
