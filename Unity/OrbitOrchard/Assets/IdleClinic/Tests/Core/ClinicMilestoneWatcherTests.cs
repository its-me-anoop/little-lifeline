using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>A goal the player has just reached is announced once, never for goals already reached when play began.</summary>
    public sealed class ClinicMilestoneWatcherTests
    {
        private static ClinicMilestone M(string id) => ClinicMilestones.Find(id);

        [Test] public void GoalsAlreadyMetWhenWatchingBeginsAreNeverAnnounced()
        {
            var watcher = new ClinicMilestoneWatcher(new[] { M("care.first"), M("care.100") });
            Assert.That(watcher.Poll(new[] { M("care.first"), M("care.100") }), Is.Empty);
        }

        [Test] public void ANewlyMetGoalIsAnnouncedExactlyOnce()
        {
            var watcher = new ClinicMilestoneWatcher(new[] { M("care.first") });
            var first = watcher.Poll(new[] { M("care.first"), M("care.100") });
            Assert.That(first.Select(m => m.Id), Is.EqualTo(new[] { "care.100" }));
            Assert.That(watcher.Poll(new[] { M("care.first"), M("care.100") }), Is.Empty);
        }

        [Test] public void AGoalThatWasClaimedAndSoLeftTheMetListIsNotAnnouncedAgain()
        {
            var watcher = new ClinicMilestoneWatcher(new[] { M("care.first") });
            watcher.Poll(new[] { M("care.100") });
            Assert.That(watcher.Poll(new ClinicMilestone[0]), Is.Empty);
            Assert.That(watcher.Poll(new[] { M("care.100") }), Is.Empty, "Seen once, so never announced twice in a session.");
        }

        [Test] public void SeveralGoalsReachedTogetherAreAllReportedInTheOrderTheyAreListed()
        {
            var watcher = new ClinicMilestoneWatcher(new ClinicMilestone[0]);
            var found = watcher.Poll(new[] { M("care.100"), M("earned.10k") });
            Assert.That(found.Select(m => m.Id), Is.EqualTo(new[] { "care.100", "earned.10k" }));
        }
    }
}
