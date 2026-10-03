using System;
using System.IO;
using IdleClinic.Progression;
using NUnit.Framework;

namespace IdleClinic.ProgressionView.Tests
{
    public sealed class ProgressionSessionTests
    {
        private string folder;
        private ProgressionSaveStore store;
        private long clock;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "lifeline-session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            store = new ProgressionSaveStore(Path.Combine(folder, "save.json"));
            clock = 1700000000;
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        private ProgressionSession Open() => new ProgressionSession(store, () => clock, new ProgressionSettings(), 8 * 3600);

        [Test]
        public void FirstLaunchIsANewGameWithNothingToReport()
        {
            var session = Open();
            Assert.IsFalse(session.Report.IsWorthShowing);
            Assert.AreEqual(50, session.Game.State.Wallet);
            Assert.AreEqual(0, session.Game.State.UpgradesPurchased);
        }

        [Test]
        public void ComingBackLaterCatchesUpAndReportsTheGap()
        {
            var first = Open();
            first.Game.Tick(200);
            first.Save();
            var upgrades = first.Game.State.UpgradesPurchased;
            clock += 1800;
            var second = Open();
            Assert.AreEqual(1800, second.Report.SecondsApplied, 1e-6);
            Assert.IsTrue(second.Report.IsWorthShowing);
            Assert.Greater(second.Game.State.UpgradesPurchased, upgrades);
        }

        [Test]
        public void AwayTimeIsCappedAtTheLimit()
        {
            var first = Open(); first.Save();
            clock += 40 * 3600;
            Assert.AreEqual(8 * 3600, Open().Report.SecondsApplied, 1e-6);
        }

        [Test]
        public void ResumingInTheSameRunCatchesUpAndMovesTheSaveForward()
        {
            var session = Open();
            session.Save();
            clock += 900;
            var report = session.Resume();
            Assert.AreEqual(900, report.SecondsApplied, 1e-6);
            clock += 100;
            Assert.AreEqual(100, session.Resume().SecondsApplied, 1e-6, "the gap is measured from the last save or resume");
        }

        [Test]
        public void ResetStartsAFreshGameAndForgetsTheOldSave()
        {
            var session = Open();
            session.Game.Tick(600);
            session.Save();
            session.Reset();
            Assert.AreEqual(50, session.Game.State.Wallet);
            Assert.AreEqual(0, session.Game.State.UpgradesPurchased);
            clock += 100;
            Assert.IsFalse(Open().Report.IsWorthShowing);
            Assert.AreEqual(50, Open().Game.State.Wallet);
        }
    }
}
