using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    public sealed class ClinicOpeningTests
    {
        private static void Task(ClinicSimulation sim,ClinicOpeningStep step)
        {
            Assert.That(sim.State.OpeningStep,Is.EqualTo(step));
            Assert.That(sim.ContinueOpening(step).Success,Is.True);
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True,"Started "+step);
            Assert.That(sim.ContinueOpening(step).Success,Is.False,"Duplicate taps cannot restart a task.");
            sim.Advance(3);
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True,"Mid-task "+step);
            sim=new ClinicSimulation(sim.State);sim.Advance(10);
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True,"Finished "+step);
        }
        private static ClinicSimulation ReadyForNurse()
        {
            var sim=ClinicSimulation.CreateOpening();
            Task(sim,ClinicOpeningStep.CleanOffice);Task(sim,ClinicOpeningStep.SetUpOffice);
            Task(sim,ClinicOpeningStep.CleanReception);Task(sim,ClinicOpeningStep.SetUpReception);
            Assert.That(sim.State.Staff,Is.Empty,"The boss covers the first check-in.");
            sim.Advance(30);
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.CleanWaiting));
            Assert.That(sim.State.TotalPayments,Is.EqualTo(1));Assert.That(sim.State.Wallet,Is.EqualTo(50));
            Task(sim,ClinicOpeningStep.CleanWaiting);Task(sim,ClinicOpeningStep.SetUpWaiting);
            Assert.That(sim.State.Room(ClinicRoom.Waiting).Built,Is.True);
            Task(sim,ClinicOpeningStep.CleanNursing);Task(sim,ClinicOpeningStep.SetUpNursing);
            return sim;
        }
        [Test]
        public void NewOpeningHasEmptyRoomsAndNoVisitorsUntilDeskIsReady()
        {
            var sim=ClinicSimulation.CreateOpening();
            Assert.That(sim.State.Rooms.All(r=>!r.Built&&r.StationCount==0),Is.True);
            Assert.That(sim.State.Patients,Is.Empty);Assert.That(sim.State.Staff,Is.Empty);
            Assert.That(sim.State.ReceptionDesks,Is.Empty);Assert.That(sim.State.TreatmentStations,Is.Empty);
            Assert.That(sim.HireNurse().Success,Is.False);Assert.That(sim.HireReceptionist().Success,Is.False);
            Assert.That(sim.ContinueOpening(ClinicOpeningStep.SetUpOffice).Success,Is.False);
            sim.AdvanceOffline(3600);
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.CleanOffice));
            Assert.That(sim.State.Patients,Is.Empty);Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
        }
        [Test]
        public void SavedOpeningRunsInOrderAndQueueWaitsForRecruitment()
        {
            var sim=ReadyForNurse();
            Assert.That(sim.State.NextArrivalTick,Is.EqualTo(-1));
            Assert.That(sim.HireReceptionist().Success,Is.False);
            Assert.That(sim.HireNurse().Success,Is.True);
            sim.Advance(120);
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.HireReceptionist));
            Assert.That(sim.State.Patients.Count(p=>!p.Paid),Is.EqualTo(5));
            Assert.That(sim.State.TotalPayments,Is.EqualTo(1));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
            sim=new ClinicSimulation(sim.State);
            Assert.That(sim.HireReceptionist().Success,Is.True);
            Assert.That(sim.State.ReceptionDesks.Count,Is.EqualTo(1),"Recruiting staffs the existing first desk.");
            Assert.That(sim.State.Tutorial,Is.EqualTo(ClinicTutorialStep.Complete));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
            sim.Advance(120);
            Assert.That(sim.State.TotalPayments,Is.GreaterThan(1));Assert.That(sim.State.TotalTreatments,Is.GreaterThan(0));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
        }
        [Test]
        public void CleaningUsesOfflineClockEvenWhenOperationsArePaused()
        {
            var sim=ClinicSimulation.CreateOpening();sim.ContinueOpening(ClinicOpeningStep.CleanOffice);
            sim.AdvanceOffline(60,0);
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.SetUpOffice));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
        }
        [Test]
        public void MissingOpeningFlagCannotLoadAnUnplayableTutorial()
        {
            var sim=ClinicSimulation.CreateNew();sim.State.Tutorial=ClinicTutorialStep.BossOpening;
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.False);
        }
        [Test]
        public void OfflineCompletionStopsAtNextPromptAndCannotSkipSetup()
        {
            var sim=ClinicSimulation.CreateOpening();sim.ContinueOpening(ClinicOpeningStep.CleanOffice);
            sim.Advance(4);sim=new ClinicSimulation(sim.State);sim.AdvanceOffline(7200);
            Assert.That(sim.State.OfficeCleaned,Is.True);Assert.That(sim.State.OfficeLevel,Is.Zero);
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.SetUpOffice));
            Assert.That(sim.State.OpeningTaskEndsTick,Is.EqualTo(-1));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
        }
    }
}
