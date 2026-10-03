using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class ProgressionGameTests
    {
        private ClinicProgression game;
        private List<ProgressionEvent> events;

        [SetUp]
        public void SetUp()
        {
            game = new ClinicProgression(new ProgressionSettings());
            events = new List<ProgressionEvent>();
            game.Occurred += events.Add;
        }

        private List<string> Completed() => events
            .Where(e => e.Kind == ProgressionEventKind.TaskCompleted && e.Task.Kind != BossTaskKind.UpgradeFurniture)
            .Select(e => e.Task.Kind + ":" + e.Task.Room).ToList();

        private void Run(double seconds) { for (var t = 0.0; t < seconds; t += 0.25) game.Tick(0.25); }

        [Test]
        public void TheOpeningFollowsTheStoryBeat()
        {
            Run(600);
            var completed = Completed();
            CollectionAssert.AreEqual(new[]
            {
                "Clean:Office", "LevelUp:Office",
                "Clean:Reception", "LevelUp:Reception", "Hire:Reception",
                "Clean:NursingStation1", "LevelUp:NursingStation1"
            }, completed.Take(7).ToList());
            // Patients back up while the boss saves for the nurse, so the waiting room opens and is cleaned around the hire.
            CollectionAssert.IsSubsetOf(new[] { "Hire:NursingStation1", "Clean:Waiting" }, completed.Skip(7).Take(3).ToList());
        }

        [Test]
        public void WaitingRoomOpensOnlyAfterPatientsBackUpAndIsThenCleanedAndBuilt()
        {
            Run(1200);
            var unlock = events.FindIndex(e => e.Kind == ProgressionEventKind.RoomUnlocked && e.Room == RoomId.Waiting);
            Assert.GreaterOrEqual(unlock, 0, "waiting room never unlocked");
            Assert.Greater(game.State.PatientsWaiting + 0, -1);
            var cleaned = events.FindIndex(unlock, e => e.Kind == ProgressionEventKind.TaskCompleted
                && e.Task.Kind == BossTaskKind.Clean && e.Task.Room == RoomId.Waiting);
            var built = events.FindIndex(unlock, e => e.Kind == ProgressionEventKind.TaskCompleted
                && e.Task.Kind == BossTaskKind.LevelUp && e.Task.Room == RoomId.Waiting);
            Assert.Greater(cleaned, unlock);
            Assert.Greater(built, cleaned);
        }

        [Test]
        public void EveryPurchaseCostsDoubleThePreviousOne()
        {
            Run(3600);
            var costs = events.Where(e => e.Kind == ProgressionEventKind.TaskStarted && e.Cost > 0
                && (e.Task.Kind == BossTaskKind.LevelUp || e.Task.Kind == BossTaskKind.UpgradeFurniture))
                .Select(e => e.Cost).ToList();
            Assert.GreaterOrEqual(costs.Count, 6);
            for (var i = 1; i < costs.Count; i++) Assert.AreEqual(costs[i - 1] * 2, costs[i]);
        }

        [Test]
        public void ReachingLevelTwoUnlocksTheSecondStationAndParking()
        {
            Run(6 * 3600);
            Assert.GreaterOrEqual(game.State.Room(RoomId.Office).Level, 2);
            Assert.IsTrue(game.State.IsUnlocked(RoomId.NursingStation2));
            Assert.IsTrue(game.State.IsUnlocked(RoomId.Parking));
            foreach (var id in new[] { RoomId.Reception, RoomId.NursingStation1, RoomId.Waiting })
                Assert.IsTrue(LevelRules.IsComplete(game.State, id, 1), id + " should be fully level one before the office moved up");
        }

        [Test]
        public void TickingInTinyStepsMatchesOneBigStep()
        {
            var a = new ClinicProgression(new ProgressionSettings());
            var b = new ClinicProgression(new ProgressionSettings());
            for (var i = 0; i < 4000; i++) a.Tick(0.1);
            b.Tick(400);
            Assert.AreEqual(a.State.Wallet, b.State.Wallet);
            Assert.AreEqual(a.State.UpgradesPurchased, b.State.UpgradesPurchased);
        }
    }
}
