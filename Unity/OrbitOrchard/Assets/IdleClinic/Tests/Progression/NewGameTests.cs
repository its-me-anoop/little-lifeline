using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class NewGameTests
    {
        [Test]
        public void EveryRoomStartsDustyWithEmptyOpenBoxes()
        {
            var state = ProgressionTestKit.NewGame();
            foreach (RoomId id in System.Enum.GetValues(typeof(RoomId)))
            {
                var room = state.Room(id);
                Assert.IsFalse(room.IsClean, id + " should be dusty");
                Assert.IsTrue(room.HasEmptyBoxes, id + " should hold open cardboard boxes");
                Assert.AreEqual(0, room.Level, id + " should be unbuilt");
            }
        }

        [Test]
        public void OnlyTheBossIsOnTheTeam()
        {
            var state = ProgressionTestKit.NewGame();
            foreach (RoomId id in System.Enum.GetValues(typeof(RoomId)))
                Assert.IsFalse(state.Room(id).StaffHired);
        }

        [Test]
        public void StartingRoomsAreOfficeReceptionAndFirstNursingStation()
        {
            var state = ProgressionTestKit.NewGame();
            Assert.IsTrue(state.IsUnlocked(RoomId.Office));
            Assert.IsTrue(state.IsUnlocked(RoomId.Reception));
            Assert.IsTrue(state.IsUnlocked(RoomId.NursingStation1));
            Assert.IsFalse(state.IsUnlocked(RoomId.Waiting));
            Assert.IsFalse(state.IsUnlocked(RoomId.NursingStation2));
            Assert.IsFalse(state.IsUnlocked(RoomId.Parking));
        }

        [Test]
        public void BossStartsWithTheSettingsWallet()
        {
            var state = ProgressionState.NewGame(new ProgressionSettings { StartingWallet = 77 });
            Assert.AreEqual(77, state.Wallet);
            Assert.AreEqual(0, state.UpgradesPurchased);
        }

        [Test]
        public void EveryRoomHasFurnitureToUpgrade()
        {
            var state = ProgressionTestKit.NewGame();
            foreach (RoomId id in System.Enum.GetValues(typeof(RoomId)))
                Assert.Greater(state.Room(id).FurnitureLevels.Length, 0);
        }
    }
}
