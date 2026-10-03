using NUnit.Framework;

namespace IdleClinic.Progression.Tests
{
    public sealed class OfflineProgressTests
    {
        [Test]
        public void TimeAwayRunsTheGameForwardAndReportsWhatChanged()
        {
            var game = new ClinicProgression(new ProgressionSettings());
            game.Tick(100);
            var before = game.State.UpgradesPurchased;
            var report = OfflineProgress.Advance(game, 1800, 8 * 3600);
            Assert.AreEqual(1800, report.SecondsApplied, 1e-6);
            Assert.Greater(game.State.UpgradesPurchased, before);
            Assert.AreEqual(game.State.UpgradesPurchased - before, report.UpgradesBought);
            Assert.AreEqual(before, report.UpgradesBefore);
        }

        [Test]
        public void TimeAwayIsCappedAtTheLimit()
        {
            var game = new ClinicProgression(new ProgressionSettings());
            var report = OfflineProgress.Advance(game, 30 * 3600, 8 * 3600);
            Assert.AreEqual(8 * 3600, report.SecondsApplied, 1e-6);
        }

        [Test]
        public void ClockGoingBackwardsOrNoGapDoesNothing()
        {
            var game = new ClinicProgression(new ProgressionSettings());
            Assert.AreEqual(0, OfflineProgress.Advance(game, -50, 8 * 3600).SecondsApplied);
            Assert.AreEqual(0, OfflineProgress.Advance(game, 0, 8 * 3600).SecondsApplied);
            Assert.AreEqual(50, game.State.Wallet);
        }

        [Test]
        public void ShortGapsAreNotWorthAReport()
        {
            var game = new ClinicProgression(new ProgressionSettings());
            Assert.IsFalse(OfflineProgress.Advance(game, 20, 8 * 3600).IsWorthShowing);
            Assert.IsTrue(OfflineProgress.Advance(game, 900, 8 * 3600).IsWorthShowing);
        }
    }
}
