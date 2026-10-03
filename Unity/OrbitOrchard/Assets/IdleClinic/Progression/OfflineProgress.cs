using System;

namespace IdleClinic.Progression
{
    public struct OfflineReport
    {
        public double SecondsApplied;
        public long WalletBefore, WalletAfter;
        public int UpgradesBefore, UpgradesBought;

        public bool IsWorthShowing => SecondsApplied >= 60;
    }

    /// <summary>Runs the game forward for the time the player was away, up to a limit.</summary>
    public static class OfflineProgress
    {
        public static OfflineReport Advance(ClinicProgression game, double secondsAway, double limitSeconds)
        {
            var applied = Math.Max(0, Math.Min(secondsAway, limitSeconds));
            var report = new OfflineReport
            {
                SecondsApplied = applied, WalletBefore = game.State.Wallet, UpgradesBefore = game.State.UpgradesPurchased
            };
            if (applied > 0) game.Tick(applied);
            report.WalletAfter = game.State.Wallet;
            report.UpgradesBought = game.State.UpgradesPurchased - report.UpgradesBefore;
            return report;
        }
    }
}
