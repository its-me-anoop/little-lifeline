using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    public sealed class ClinicOfflineLimitTests
    {
        private static ClinicSimulation Running()
        {
            var game = ClinicSimulation.CreateNew(); game.Advance(25); game.Collect(0); game.HireNurse(); game.Advance(30);
            return game;
        }

        [Test] public void TheTillsStopEarningAtTheirCoinLimitWhileAway()
        {
            var game = Running();
            var cap = ClinicRules.OfflineCoinCap(game.State);
            Assert.That(cap, Is.EqualTo(250 * ClinicRules.VisitFee(game.State)), "Rules 4: about 250 visits' fees.");
            var earned = game.State.TotalEarned;
            var report = game.AdvanceOffline(8 * 3600, ClinicRules.MaximumOfflineSeconds, cap);
            Assert.That(report.CoinCapped, Is.True);
            Assert.That(game.State.TotalEarned - earned, Is.InRange(cap, cap + 2 * ClinicRules.MaximumVisitFee(game.State)), "Earning stops within a visit or two of the limit.");
            Assert.That(report.EarningsSeconds, Is.LessThan(8 * 3600));
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
        }

        [Test] public void ALongerOfflineLimitKeepsEarningPastEightHours()
        {
            var eight = Running(); var twelve = Running();
            var a = eight.AdvanceOffline(12 * 3600);
            var b = twelve.AdvanceOffline(12 * 3600, 12 * 3600);
            Assert.That(a.EarningsSeconds, Is.EqualTo(8 * 3600).Within(.01));
            Assert.That(b.EarningsSeconds, Is.EqualTo(12 * 3600).Within(.01));
            Assert.That(twelve.State.TotalEarned, Is.GreaterThan(eight.State.TotalEarned));
            Assert.That(ClinicSimulation.IsValidState(twelve.State), Is.True);
        }

        [Test] public void RewardsPayIntoTheirOwnLedgerAndKeepTheClinicValid()
        {
            var game = Running(); var wallet = game.State.Wallet;
            Assert.That(ClinicSimulation.TryGrantReward(game.State, 1234), Is.True);
            Assert.That(game.State.Wallet, Is.EqualTo(wallet + 1234));
            Assert.That(game.State.TotalRewards, Is.EqualTo(1234));
            Assert.That(game.State.TotalEarned, Is.EqualTo(game.State.TotalEarned), "Patient income is untouched.");
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
            Assert.That(ClinicSimulation.TryGrantReward(game.State, 0), Is.False);
            game.State.Wallet += 1;
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.False, "Coins from nowhere are still rejected.");
        }
    }
}
