using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class UnlockRuleTests
    {
        [Test]
        public void WaitingRoomStaysLockedUntilMoreThanTwoPatientsWait()
        {
            var state = ProgressionTestKit.NewGame();
            state.PatientsWaiting = 2;
            Assert.IsEmpty(UnlockRules.Apply(state));
            Assert.IsFalse(state.IsUnlocked(RoomId.Waiting));
        }

        [Test]
        public void ThreeWaitingPatientsUnlockTheWaitingRoom()
        {
            var state = ProgressionTestKit.NewGame();
            state.PatientsWaiting = 3;
            CollectionAssert.AreEqual(new[] { RoomId.Waiting }, UnlockRules.Apply(state));
            Assert.IsTrue(state.IsUnlocked(RoomId.Waiting));
        }

        [Test]
        public void WaitingRoomStaysUnlockedWhenThePatientsLeave()
        {
            var state = ProgressionTestKit.NewGame();
            state.PatientsWaiting = 3;
            UnlockRules.Apply(state);
            state.PatientsWaiting = 0;
            Assert.IsEmpty(UnlockRules.Apply(state));
            Assert.IsTrue(state.IsUnlocked(RoomId.Waiting));
        }

        [Test]
        public void OfficeLevelTwoUnlocksTheSecondNursingStationAndParking()
        {
            var state = ProgressionTestKit.NewGame();
            state.Room(RoomId.Office).Level = 1;
            Assert.IsEmpty(UnlockRules.Apply(state));
            state.Room(RoomId.Office).Level = 2;
            CollectionAssert.AreEquivalent(new[] { RoomId.NursingStation2, RoomId.Parking }, UnlockRules.Apply(state));
        }
    }
}
