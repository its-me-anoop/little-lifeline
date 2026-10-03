using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Progression.Tests
{
    public sealed class SnapshotTests
    {
        private static ClinicProgression PlayedFor(double seconds)
        {
            var game = new ClinicProgression(new ProgressionSettings());
            game.Tick(seconds);
            return game;
        }

        private static void AssertSame(ClinicProgression a, ClinicProgression b)
        {
            Assert.AreEqual(a.State.Wallet, b.State.Wallet, "wallet");
            Assert.AreEqual(a.State.UpgradesPurchased, b.State.UpgradesPurchased, "upgrades");
            Assert.AreEqual(a.State.PatientsWaiting, b.State.PatientsWaiting, "waiting");
            foreach (RoomId id in System.Enum.GetValues(typeof(RoomId)))
            {
                Assert.AreEqual(a.State.IsUnlocked(id), b.State.IsUnlocked(id), id + " unlocked");
                Assert.AreEqual(a.State.Room(id).Level, b.State.Room(id).Level, id + " level");
                Assert.AreEqual(a.State.Room(id).IsClean, b.State.Room(id).IsClean, id + " clean");
                Assert.AreEqual(a.State.Room(id).StaffHired, b.State.Room(id).StaffHired, id + " staff");
                CollectionAssert.AreEqual(a.State.Room(id).FurnitureLevels, b.State.Room(id).FurnitureLevels, id + " furniture");
            }
        }

        [Test]
        public void ARestoredGameContinuesExactlyLikeTheOriginal()
        {
            var original = PlayedFor(931.3); // stops mid task, mid treatment, mid arrival
            var restored = new ClinicProgression(new ProgressionSettings());
            restored.Restore(original.Capture());
            AssertSame(original, restored);
            original.Tick(3600); restored.Tick(3600);
            AssertSame(original, restored);
        }

        [Test]
        public void ARestoredBossKeepsHisTaskAndPlace()
        {
            var original = PlayedFor(12.5);
            var restored = new ClinicProgression(new ProgressionSettings());
            restored.Restore(original.Capture());
            Assert.AreEqual(original.BossLocation, restored.BossLocation);
            Assert.AreEqual(original.BossTask.HasValue, restored.BossTask.HasValue);
            if (original.BossTask.HasValue)
            {
                Assert.AreEqual(original.BossTask.Value.Kind, restored.BossTask.Value.Kind);
                Assert.AreEqual(original.BossTask.Value.Room, restored.BossTask.Value.Room);
                Assert.AreEqual(original.BossProgress, restored.BossProgress, 1e-6);
            }
        }

        [Test]
        public void ASnapshotSurvivesJson()
        {
            var original = PlayedFor(2000);
            var json = JsonUtility.ToJson(original.Capture());
            var restored = new ClinicProgression(new ProgressionSettings());
            restored.Restore(JsonUtility.FromJson<ProgressionSnapshot>(json));
            AssertSame(original, restored);
            original.Tick(1800); restored.Tick(1800);
            AssertSame(original, restored);
        }

        [Test]
        public void RestoringAnEmptySnapshotLeavesANewGame()
        {
            var game = new ClinicProgression(new ProgressionSettings());
            game.Restore(null);
            AssertSame(game, new ClinicProgression(new ProgressionSettings()));
        }
    }
}
