using System.Collections.Generic;
using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class BossWorkerTests
    {
        private ProgressionSettings settings;
        private ProgressionState state;
        private BossWorker boss;
        private List<ProgressionEvent> events;

        [SetUp]
        public void SetUp()
        {
            settings = new ProgressionSettings
            {
                StartingWallet = 100, BaseUpgradeCost = 10, HireCost = 15,
                CleanSeconds = 5, BuildSeconds = 4, FurnitureSeconds = 2, HireSeconds = 1, TravelSeconds = 0
            };
            state = ProgressionState.NewGame(settings);
            boss = new BossWorker(state, new BossPlanner(), new DoublingCostPolicy(settings.BaseUpgradeCost), settings);
            events = new List<ProgressionEvent>();
            boss.Occurred += events.Add;
        }

        [Test]
        public void BossStartsCleaningTheOfficeAndFinishesAfterTheCleaningTime()
        {
            boss.Tick(0.1);
            Assert.AreEqual(BossTaskKind.Clean, boss.Current.Value.Kind);
            Assert.IsFalse(state.Room(RoomId.Office).IsClean);
            boss.Tick(4.8);
            Assert.IsFalse(state.Room(RoomId.Office).IsClean);
            boss.Tick(0.2);
            Assert.IsTrue(state.Room(RoomId.Office).IsClean);
            Assert.IsFalse(state.Room(RoomId.Office).HasEmptyBoxes);
        }

        [Test]
        public void CleaningIsFree()
        {
            boss.Tick(5.0);
            Assert.AreEqual(100, state.Wallet);
        }

        [Test]
        public void BuildingPaysTheFirstUpgradePriceUpFront()
        {
            boss.Tick(5.1); // clean
            boss.Tick(0.1); // starts building
            Assert.AreEqual(BossTaskKind.LevelUp, boss.Current.Value.Kind);
            Assert.AreEqual(90, state.Wallet);
            Assert.AreEqual(0, state.Room(RoomId.Office).Level);
            boss.Tick(4.0);
            Assert.AreEqual(1, state.Room(RoomId.Office).Level);
            Assert.AreEqual(1, state.UpgradesPurchased);
        }

        [Test]
        public void TheNextUpgradeCostsDouble()
        {
            boss.Tick(5.1); boss.Tick(4.1); // office level 1, paid 10
            boss.Tick(5.1);                 // reception cleaned
            boss.Tick(0.1);                 // reception build starts
            Assert.AreEqual(100 - 10 - 20, state.Wallet);
        }

        [Test]
        public void HiringCostsAFixedFeeAndDoesNotRaiseUpgradePrices()
        {
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Reception).IsClean = true;
            state.Room(RoomId.Reception).Level = 1;
            state.UpgradesPurchased = 2;
            boss.Tick(0.1);
            Assert.AreEqual(BossTaskKind.Hire, boss.Current.Value.Kind);
            Assert.AreEqual(85, state.Wallet);
            boss.Tick(1.0);
            Assert.IsTrue(state.Room(RoomId.Reception).StaffHired);
            Assert.AreEqual(2, state.UpgradesPurchased);
        }

        [Test]
        public void BossWaitsWhenHeCannotAffordTheNextStepThenGoesAheadOnceFundsArrive()
        {
            state.Wallet = 5;
            boss.Tick(5.1); // office cleaned, build costs 10
            boss.Tick(3.0);
            Assert.IsNull(boss.Current);
            Assert.AreEqual(0, state.Room(RoomId.Office).Level);
            Assert.AreEqual(5, state.Wallet);

            state.Wallet = 10;
            boss.Tick(0.1);
            Assert.AreEqual(BossTaskKind.LevelUp, boss.Current.Value.Kind);
            Assert.AreEqual(0, state.Wallet);
        }

        [Test]
        public void LargeTimeStepsCarryOnThroughSeveralTasks()
        {
            boss.Tick(60);
            Assert.AreEqual(1, state.Room(RoomId.Office).Level);
            Assert.IsTrue(state.Room(RoomId.Reception).IsClean);
        }

        [Test]
        public void EventsAnnounceStartsAndFinishesInOrder()
        {
            boss.Tick(5.1);
            Assert.AreEqual(ProgressionEventKind.TaskStarted, events[0].Kind);
            Assert.AreEqual(ProgressionEventKind.TaskCompleted, events[1].Kind);
            Assert.AreEqual(BossTaskKind.Clean, events[1].Task.Kind);
            Assert.AreEqual(RoomId.Office, events[1].Task.Room);
        }

        [Test]
        public void FinishingAHireMarksTheStaffMemberAsWorking()
        {
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.NursingStation1).IsClean = true;
            state.Room(RoomId.NursingStation1).Level = 1;
            state.Room(RoomId.Reception).IsClean = true;
            state.Room(RoomId.Reception).Level = 1;
            state.Room(RoomId.Reception).StaffHired = true;
            boss.Tick(1.1);
            Assert.IsTrue(state.Room(RoomId.NursingStation1).StaffHired);
        }

        [Test]
        public void UnlocksAreEvaluatedAsRoomsProgress()
        {
            ProgressionTestKit.BuildStarterRooms(state, 1);
            ProgressionTestKit.Complete(state, RoomId.Waiting, 1);
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            state.Wallet = 1000;
            state.UpgradesPurchased = 3;
            boss.Tick(4.1); // office to level 2
            Assert.IsTrue(state.IsUnlocked(RoomId.Parking));
            Assert.IsTrue(events.Exists(e => e.Kind == ProgressionEventKind.RoomUnlocked && e.Room == RoomId.Parking));
        }

        [Test]
        public void BossStartsInTheOffice()
        {
            Assert.AreEqual(RoomId.Office, boss.Location);
            Assert.IsFalse(boss.IsTravelling);
        }

        [Test]
        public void BossWalksToTheNextRoomBeforeHeStartsWorkThere()
        {
            settings.TravelSeconds = 2;
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            boss.Tick(0.1);
            Assert.IsTrue(boss.IsTravelling);
            Assert.AreEqual(RoomId.Reception, boss.Destination);
            Assert.IsNull(boss.Current);
            Assert.AreEqual(100, state.Wallet);
            boss.Tick(0.9);
            Assert.AreEqual(0.5, boss.TravelProgress, 0.001);
            boss.Tick(1.1);
            Assert.AreEqual(RoomId.Reception, boss.Location);
            Assert.IsFalse(boss.IsTravelling);
            Assert.AreEqual(BossTaskKind.Clean, boss.Current.Value.Kind);
        }

        [Test]
        public void NoTravelWhenTheNextJobIsInTheRoomHeIsIn()
        {
            settings.TravelSeconds = 2;
            boss.Tick(0.1);
            Assert.IsFalse(boss.IsTravelling);
            Assert.AreEqual(BossTaskKind.Clean, boss.Current.Value.Kind);
        }

        [Test]
        public void BossDoesNotWalkAwayWhenHeCannotAffordTheNextStep()
        {
            settings.TravelSeconds = 2;
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            state.Room(RoomId.Reception).IsClean = true;
            state.Room(RoomId.Reception).Level = 1; // next step is hiring, which costs 15
            state.Wallet = 5;
            boss.Tick(5.0);
            Assert.AreEqual(RoomId.Office, boss.Location);
            Assert.IsFalse(boss.IsTravelling);
        }

        [Test]
        public void StartingToWalkIsAnnounced()
        {
            settings.TravelSeconds = 2;
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            boss.Tick(0.1);
            Assert.IsTrue(events.Exists(e => e.Kind == ProgressionEventKind.BossTravelStarted && e.Room == RoomId.Reception));
        }
    }
}
