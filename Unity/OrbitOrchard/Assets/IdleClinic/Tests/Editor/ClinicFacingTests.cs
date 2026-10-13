using System;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>People look at who they are dealing with: the visitor and the staff member at a desk or a treatment bay face
    /// each other, and a seated visitor faces the way the chair does.</summary>
    public sealed class ClinicFacingTests
    {
        private GameObject host;
        private ClinicWorld world;
        [SetUp] public void SetUp(){host=new GameObject("Clinic facing test");world=host.AddComponent<ClinicWorld>();world.Initialize();}
        [TearDown] public void TearDown()=>UnityEngine.Object.DestroyImmediate(host);

        private Transform Find(string name)=>Array.Find(host.GetComponentsInChildren<Transform>(true),t=>t.name==name);
        private ClinicState Stationary(ClinicPatientPhase phase,string patientAnchor,ClinicStaffRole role,string staffAnchor)
        {
            var state=ClinicSimulation.CreateNew().State;state.Patients.Clear();state.Staff.Clear();
            state.Room(ClinicRoom.Waiting).Built=true;
            state.Patients.Add(new ClinicPatientState{Id=1,Phase=phase,FromAnchor=patientAnchor,ToAnchor=patientAnchor,PhaseEndsTick=1000});
            state.Staff.Add(new ClinicStaffState{Id=101,Role=role,PatientId=1,FromAnchor=staffAnchor,ToAnchor=staffAnchor,MoveEndsTick=0});
            return state;
        }
        private static float Look(Transform from,Transform to){var d=to.position-from.position;d.y=0;return Vector3.Dot(from.forward,d.normalized);}

        // A patient lying back in the treatment chair stays square to the chair; the nurse turns to the patient.
        [TestCase(ClinicPatientPhase.CheckingIn,"reception.desk.0.patient",ClinicStaffRole.Receptionist,"reception.desk.0.staff",true)]
        [TestCase(ClinicPatientPhase.Treating,"firstaid.station.0.patient",ClinicStaffRole.Nurse,"firstaid.station.0.staff",false)]
        public void VisitorAndStaffMemberFaceEachOtherAtTheirStation(ClinicPatientPhase phase,string patientAnchor,ClinicStaffRole role,string staffAnchor,bool visitorFacesStaff)
        {
            var state=Stationary(phase,patientAnchor,role,staffAnchor);
            for(int tick=0;tick<30;tick++){state.Tick=tick;world.Render(state,.1f,false);}
            var patient=Find("Patient 1");var member=Find("Staff 101");
            Assert.That(Look(member,patient),Is.GreaterThan(.5f),"The staff member looks at the visitor.");
            if(visitorFacesStaff)Assert.That(Look(patient,member),Is.GreaterThan(.5f),"The visitor looks at the staff member.");
        }

        private Transform Named(string name,Transform within=null)
        {
            var found=Array.Find((within==null?host.transform:within).GetComponentsInChildren<Transform>(true),t=>t.name==name);
            Assert.That(found,Is.Not.Null,name);return found;
        }

        [Test] public void TheOfficeComputerFacesTheBossNotTheVisitors()
        {
            var monitor=Named("Computer monitor");var screen=Named("Computer screen");var keyboard=Named("Keyboard");var chair=Named("Boss swivel chair");
            Assert.That(Mathf.Sign(screen.position.z-monitor.position.z),Is.EqualTo(Mathf.Sign(chair.position.z-monitor.position.z)),"The screen is on the boss's side of the monitor.");
            Assert.That(Mathf.Sign(keyboard.position.z-monitor.position.z),Is.EqualTo(Mathf.Sign(chair.position.z-monitor.position.z)),"The keyboard is in front of the boss.");
        }

        [Test] public void TheBossDeskDrawersFaceTheBoss()
        {
            var desk=Named("Boss desk");var chair=Named("Boss swivel chair");
            var body=Named("Cabinet body",desk);var handle=Named("Cabinet brass handle",desk);
            Assert.That(Mathf.Sign(handle.position.z-body.position.z),Is.EqualTo(Mathf.Sign(chair.position.z-body.position.z)));
        }

        [Test] public void CabinetsAgainstTheWestWallOpenIntoTheRoom()
        {
            foreach(var unit in new[]{"Reception record storage","Care supplies cabinet","Treatment handwash sink"})
            {
                var root=Named(unit);var body=Named("Cabinet body",root);var handle=Named("Cabinet brass handle",root);
                Assert.That(handle.position.x,Is.GreaterThan(body.position.x+.2f),unit+" has its doors towards the room, not the wall.");
            }
        }

        [Test] public void TheReceptionNoticeBoardShowsItsPostersToTheRoom()
        {
            var board=Named("Reception notice board");var cork=Named("Notice cork",board);
            Assert.That(cork.position.x,Is.GreaterThan(board.position.x),"The cork faces east, into the room, not into the west wall.");
        }
    }
}
