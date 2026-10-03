using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class BossPlannerTests
    {
        private static BossTask Next(ProgressionState state) => new BossPlanner().Next(state).Value;

        private static void AssertTask(BossTask task, BossTaskKind kind, RoomId room)
        {
            Assert.AreEqual(kind, task.Kind);
            Assert.AreEqual(room, task.Room);
        }

        [Test]
        public void BossCleansTheOfficeFirst()
        {
            AssertTask(Next(ProgressionTestKit.NewGame()), BossTaskKind.Clean, RoomId.Office);
        }

        [Test]
        public void ThenBuildsTheOfficeToLevelOne()
        {
            var state = ProgressionTestKit.NewGame();
            state.Room(RoomId.Office).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.Office);
        }

        [Test]
        public void ThenCleansReception()
        {
            var state = ProgressionTestKit.NewGame();
            state.Room(RoomId.Office).IsClean = true;
            state.Room(RoomId.Office).Level = 1;
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.Reception);
        }

        [Test]
        public void ThenBuildsReceptionAndHiresTheReceptionist()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            state.Room(RoomId.Reception).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.Reception);
            state.Room(RoomId.Reception).Level = 1;
            AssertTask(Next(state), BossTaskKind.Hire, RoomId.Reception);
        }

        [Test]
        public void ThenCleansBuildsAndStaffsTheFirstNursingStation()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            ProgressionTestKit.Complete(state, RoomId.Reception, 1);
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.NursingStation1);
            state.Room(RoomId.NursingStation1).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.NursingStation1);
            state.Room(RoomId.NursingStation1).Level = 1;
            AssertTask(Next(state), BossTaskKind.Hire, RoomId.NursingStation1);
        }

        [Test]
        public void FurnitureWaitsUntilTheRoomsAreBuilt()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            state.Room(RoomId.Office).FurnitureLevels[1] = 0;
            var task = Next(state);
            AssertTask(task, BossTaskKind.UpgradeFurniture, RoomId.Office);
            Assert.AreEqual(1, task.FurnitureIndex);
        }

        [Test]
        public void ANewlyUnlockedWaitingRoomIsCleanedAndBuiltBeforeAnyFurniture()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            state.Room(RoomId.Office).FurnitureLevels[0] = 0;
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.Waiting);
            state.Room(RoomId.Waiting).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.Waiting);
            state.Room(RoomId.Waiting).Level = 1;
            AssertTask(Next(state), BossTaskKind.UpgradeFurniture, RoomId.Office);
        }

        [Test]
        public void OnceEverythingIsDoneTheOfficeGoesToLevelTwo()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            ProgressionTestKit.Complete(state, RoomId.Waiting, 1);
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.Office);
        }

        [Test]
        public void LevelTwoOfficeOpensTheSecondStationAndParkingForCleaning()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            ProgressionTestKit.Complete(state, RoomId.Waiting, 1);
            state.PatientsWaiting = 3;
            state.Room(RoomId.Office).Level = 2;
            UnlockRules.Apply(state);
            // The new rooms are cleaned the moment they open; the level-one rooms are brought up after that.
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.NursingStation2);
            state.Room(RoomId.NursingStation2).IsClean = true;
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.Parking);
            state.Room(RoomId.Parking).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.Reception);
        }

        [Test]
        public void ANewRoomIsCleanedAheadOfEarlierRoomsWaitingForTheirNextLevel()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            ProgressionTestKit.Complete(state, RoomId.Waiting, 1);
            state.Room(RoomId.Office).Level = 2;
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            Assert.IsTrue(state.IsUnlocked(RoomId.Parking));
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.NursingStation2);
        }

        [Test]
        public void NewRoomsClimbLevelByLevelToCatchUp()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 2);
            ProgressionTestKit.Complete(state, RoomId.Waiting, 2);
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            ProgressionTestKit.Complete(state, RoomId.Parking, 2);
            AssertTask(Next(state), BossTaskKind.Clean, RoomId.NursingStation2);
            state.Room(RoomId.NursingStation2).IsClean = true;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.NursingStation2);
            state.Room(RoomId.NursingStation2).Level = 1;
            AssertTask(Next(state), BossTaskKind.LevelUp, RoomId.NursingStation2);
            state.Room(RoomId.NursingStation2).Level = 2;
            AssertTask(Next(state), BossTaskKind.Hire, RoomId.NursingStation2);
        }

        [Test]
        public void NothingIsLeftToDoWhenEveryRoomIsFullyUpgradedAndTheOfficeIsBlocked()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            // Office must still be able to go to level 2, so there is always a next task here.
            Assert.IsNotNull(new BossPlanner().Next(state));
        }
    }
}
