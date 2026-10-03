using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class PatientFlowTests
    {
        private ProgressionSettings settings;
        private ProgressionState state;
        private PatientFlow flow;

        [SetUp]
        public void SetUp()
        {
            settings = new ProgressionSettings
            {
                StartingWallet = 0, BaseWaitingCapacity = 1000, ArrivalSeconds = 4, CheckInFee = 10, TreatmentSeconds = 8, TreatmentFee = 12
            };
            state = ProgressionState.NewGame(settings);
            flow = new PatientFlow(state, settings);
        }

        private void OpenReception() => ProgressionTestKit.Complete(state, RoomId.Reception, 1);
        private void OpenNursing(RoomId id) => ProgressionTestKit.Complete(state, id, 1);

        [Test]
        public void NobodyArrivesBeforeReceptionHasItsReceptionist()
        {
            flow.Tick(60);
            Assert.AreEqual(0, state.PatientsWaiting);
            Assert.AreEqual(0, state.Wallet);
        }

        [Test]
        public void ReceptionistChecksPatientsInAndTheyPayTheFee()
        {
            OpenReception();
            flow.Tick(4.1);
            Assert.AreEqual(10, state.Wallet);
            Assert.AreEqual(1, state.PatientsWaiting);
        }

        [Test]
        public void PatientsPileUpWhileNoNurseIsHired()
        {
            OpenReception();
            flow.Tick(12.5);
            Assert.AreEqual(3, state.PatientsWaiting);
        }

        [Test]
        public void ANurseTakesTheNextPatientAndIsPaidWhenTreatmentEnds()
        {
            settings.ArrivalSeconds = 10;
            flow = new PatientFlow(state, settings);
            OpenReception();
            OpenNursing(RoomId.NursingStation1);
            flow.Tick(10.1);                 // first patient arrives and the nurse starts at once
            Assert.AreEqual(0, state.PatientsWaiting);
            Assert.AreEqual(10, state.Wallet);
            flow.Tick(8.0);
            Assert.AreEqual(10 + 12, state.Wallet);
        }

        [Test]
        public void ANurseTreatsOnePatientAtATime()
        {
            OpenReception();
            OpenNursing(RoomId.NursingStation1);
            flow.Tick(4.1);
            flow.Tick(4.0); // second patient arrives while first is mid treatment
            Assert.AreEqual(1, state.PatientsWaiting);
        }

        [Test]
        public void ASecondStationDoublesTreatmentCapacity()
        {
            OpenReception();
            OpenNursing(RoomId.NursingStation1);
            OpenNursing(RoomId.NursingStation2);
            flow.Tick(4.1);
            flow.Tick(4.0);
            Assert.AreEqual(0, state.PatientsWaiting);
        }

        [Test]
        public void ParkingBringsPatientsInFaster()
        {
            OpenReception();
            flow.Tick(40);
            var withoutParking = state.Wallet;

            var other = ProgressionState.NewGame(settings);
            var otherFlow = new PatientFlow(other, settings);
            ProgressionTestKit.Complete(other, RoomId.Reception, 1);
            ProgressionTestKit.Complete(other, RoomId.Parking, 1);
            otherFlow.Tick(40);
            Assert.Greater(other.Wallet, withoutParking);
        }

        [Test]
        public void EveryUpgradeBoughtRaisesWhatPatientsPay()
        {
            settings.IncomeBoostPerUpgrade = 0.5;
            OpenReception();
            state.UpgradesPurchased = 4; // 1 + 4 * 0.5 = x3
            flow.Tick(4.1);
            Assert.AreEqual(30, state.Wallet);
        }

        [Test]
        public void ArrivalsAreAnnouncedForThePresentation()
        {
            OpenReception();
            var arrivals = 0;
            flow.Occurred += e => { if (e.Kind == ProgressionEventKind.PatientArrived) arrivals++; };
            flow.Tick(8.5);
            Assert.AreEqual(2, arrivals);
        }

        [Test]
        public void ReceptionOnlyHoldsAFewWaitingPatients()
        {
            settings.BaseWaitingCapacity = 4;
            OpenReception();
            flow.Tick(60);
            Assert.AreEqual(4, state.PatientsWaiting);
        }

        [Test]
        public void PatientsTurnedAwayPayNothingAndAreAnnounced()
        {
            settings.BaseWaitingCapacity = 2;
            OpenReception();
            var turnedAway = 0;
            flow.Occurred += e => { if (e.Kind == ProgressionEventKind.PatientTurnedAway) turnedAway++; };
            flow.Tick(16.5); // four arrivals, room for two
            Assert.AreEqual(2, state.PatientsWaiting);
            Assert.AreEqual(2, turnedAway);
            Assert.AreEqual(20, state.Wallet);
        }

        [Test]
        public void ABuiltWaitingRoomAddsRoomForMorePatientsPerLevel()
        {
            settings.BaseWaitingCapacity = 4; settings.WaitingCapacityPerLevel = 3;
            OpenReception();
            state.Unlock(RoomId.Waiting);
            state.Room(RoomId.Waiting).Level = 2;
            Assert.AreEqual(10, flow.Capacity);
            flow.Tick(200);
            Assert.AreEqual(10, state.PatientsWaiting);
        }

        [Test]
        public void ALockedWaitingRoomAddsNothing()
        {
            settings.BaseWaitingCapacity = 4; settings.WaitingCapacityPerLevel = 3;
            state.Room(RoomId.Waiting).Level = 2; // never unlocked
            Assert.AreEqual(4, flow.Capacity);
        }
    }
}
