using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    public sealed class ClinicOfficeTests
    {
        private static ClinicSimulation Ready()
        {
            var sim=ClinicSimulation.CreateOpening();
            for(var i=0;i<30&&sim.State.OpeningStep!=ClinicOpeningStep.Complete;i++)
            {
                var step=sim.State.OpeningStep;
                if(ClinicSimulation.OpeningManual(step)||step==ClinicOpeningStep.HireNurse||step==ClinicOpeningStep.HireReceptionist)
                    Assert.That(sim.ContinueOpening(step).Success,Is.True);
                sim.Advance(30);
            }
            Assert.That(sim.State.OpeningStep,Is.EqualTo(ClinicOpeningStep.Complete));
            sim.State.Wallet+=10000000;sim.State.TotalRewards+=10000000;
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
            return sim;
        }
        [Test]
        public void OfficeUnlocksOneLevelAndWaitsForEveryCompletedRoom()
        {
            var sim=Ready();
            Assert.That(sim.Renovate(ClinicRoom.Reception).Success,Is.False);
            Assert.That(sim.UpgradeOffice().Success,Is.True);
            var spent=sim.State.TotalSpent;
            Assert.That(sim.UpgradeOffice().Success,Is.False);
            Assert.That(sim.State.TotalSpent,Is.EqualTo(spent));
            Assert.That(sim.State.OfficeLevel,Is.EqualTo(1));
            Assert.That(sim.BuildersBusy,Is.True);
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
            sim.Advance(sim.OfficeUpgradeSeconds);
            Assert.That(sim.State.OfficeLevel,Is.EqualTo(2));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
            foreach(var room in sim.State.Rooms)
            {
                Assert.That(sim.UpgradeOffice().Success,Is.False);
                Assert.That(sim.Renovate(room.Kind).Success,Is.True);
                Assert.That(sim.UpgradeOffice().Success,Is.False,"Paid unfinished work is not a completed level.");
                Assert.That(sim.CompleteConstructionNow(sim.State.Construction.Single().Id).Success,Is.True);
                Assert.That(sim.Renovate(room.Kind).Success,Is.False,"Rooms cannot outrun the office.");
            }
            Assert.That(sim.UpgradeOffice().Success,Is.True);
            sim.AdvanceOffline(3600,0);
            Assert.That(sim.State.OfficeLevel,Is.EqualTo(3));
            Assert.That(ClinicSimulation.IsValidState(sim.State),Is.True);
        }
        [Test]
        public void SavedOfficeTimerResumesWithoutChargingAgainAndRejectsInvalidRoomLevels()
        {
            var sim=Ready();sim.UpgradeOffice();sim.Advance(1);
            var json=UnityEngine.JsonUtility.ToJson(sim.State);
            var state=UnityEngine.JsonUtility.FromJson<ClinicState>(json);
            Assert.That(ClinicSimulation.IsValidState(state),Is.True);
            var spent=state.TotalSpent;
            sim=new ClinicSimulation(state);sim.AdvanceOffline(3600,0);
            Assert.That(state.OfficeLevel,Is.EqualTo(2));Assert.That(state.TotalSpent,Is.EqualTo(spent));
            Assert.That(ClinicSimulation.IsValidState(state),Is.True);
            state.Room(ClinicRoom.Waiting).Tier=3;
            Assert.That(ClinicSimulation.IsValidState(state),Is.False);
        }
        [Test]
        public void ExistingClinicAdoptsHighestCompletedOrPaidLevelWithoutLosingProgress()
        {
            var sim=ClinicSimulation.CreateNew();
            sim.State.Room(ClinicRoom.Reception).Tier=4;
            sim.State.Room(ClinicRoom.FirstAid).Tier=2;
            var wallet=sim.State.Wallet;
            sim.EnsureOfficeAnchor();
            Assert.That(sim.State.OfficeLevel,Is.EqualTo(4));
            Assert.That(sim.State.Room(ClinicRoom.FirstAid).Tier,Is.EqualTo(2));
            Assert.That(sim.State.Wallet,Is.EqualTo(wallet));
            sim.EnsureOfficeAnchor();Assert.That(sim.State.OfficeLevel,Is.EqualTo(4));
            var pending=ClinicSimulation.CreateNew();
            pending.State.Construction.Add(new ClinicConstructionState{Room=ClinicRoom.Reception,TargetTier=5});
            pending.EnsureOfficeAnchor();
            Assert.That(pending.State.OfficeLevel,Is.EqualTo(5));
            Assert.That(pending.State.Construction.Single().TargetTier,Is.EqualTo(5));
        }
    }
}
