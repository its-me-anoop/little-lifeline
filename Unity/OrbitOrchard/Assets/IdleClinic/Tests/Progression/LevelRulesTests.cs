using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class LevelRulesTests
    {
        [Test]
        public void OfficeCanBeBuiltOnceItIsClean()
        {
            var state = ProgressionTestKit.NewGame();
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office));
            state.Room(RoomId.Office).IsClean = true;
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Office));
        }

        [Test]
        public void OtherRoomsNeedTheOfficeToBeAtLeastOneLevelAhead()
        {
            var state = ProgressionTestKit.NewGame();
            state.Room(RoomId.Reception).IsClean = true;
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Reception), "office not built yet");
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Reception));
            state.Room(RoomId.Reception).Level = 1;
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Reception), "office is still level 1");
        }

        [Test]
        public void OfficeWaitsForEveryUnlockedRoomToBeFullyUpgraded()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Office));

            state.Room(RoomId.Reception).FurnitureLevels[0] = 0;
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office), "a furniture upgrade is outstanding");
        }

        [Test]
        public void UnlockedWaitingRoomHoldsTheOfficeBackUntilItIsDone()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office));
            ProgressionTestKit.Complete(state, RoomId.Waiting, 1);
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Office));
        }

        [Test]
        public void LockedRoomsDoNotHoldTheOfficeBack()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            Assert.IsFalse(state.IsUnlocked(RoomId.Waiting));
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Office));
        }

        [Test]
        public void ARoomWithoutItsStaffIsNotFullyUpgraded()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 1);
            state.Room(RoomId.NursingStation1).StaffHired = false;
            Assert.IsFalse(LevelRules.IsComplete(state, RoomId.NursingStation1, 1));
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office));
        }

        [Test]
        public void OfficeLevelThreeFollowsEveryRoomBeingFullyAtLevelTwo()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.BuildStarterRooms(state, 2);
            UnlockRules.Apply(state);
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office), "second station and parking are still unbuilt");
            ProgressionTestKit.Complete(state, RoomId.NursingStation2, 2);
            Assert.IsFalse(LevelRules.CanLevelUp(state, RoomId.Office));
            ProgressionTestKit.Complete(state, RoomId.Parking, 2);
            Assert.IsTrue(LevelRules.CanLevelUp(state, RoomId.Office));
        }

        [Test]
        public void FurnitureCannotOutrunItsRoom()
        {
            var state = ProgressionTestKit.NewGame();
            ProgressionTestKit.Complete(state, RoomId.Office, 1);
            state.Room(RoomId.Reception).IsClean = true;
            state.Room(RoomId.Reception).Level = 1;
            Assert.IsTrue(LevelRules.CanUpgradeFurniture(state, RoomId.Reception, 0));
            state.Room(RoomId.Reception).FurnitureLevels[0] = 1;
            Assert.IsFalse(LevelRules.CanUpgradeFurniture(state, RoomId.Reception, 0));
        }
    }
}
