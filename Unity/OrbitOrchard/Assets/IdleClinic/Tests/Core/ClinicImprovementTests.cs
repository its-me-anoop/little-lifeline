using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>After each purchase the player sees what it changed, before and after, and one readable next step.</summary>
    public sealed class ClinicImprovementTests
    {
        private static ClinicSimulation OpenClinic()
        {
            var sim = ClinicSimulation.CreateOpening();
            for (var guard = 0; guard < 400 && sim.State.OpeningStep != ClinicOpeningStep.Complete; guard++)
            {
                var step = sim.State.OpeningStep;
                if (sim.State.OpeningTaskEndsTick < 0 && step != ClinicOpeningStep.FirstCheckIn && step != ClinicOpeningStep.QueueArrivals) sim.ContinueOpening(step);
                sim.Advance(1, false);
            }
            ClinicSimulation.TryGrantReward(sim.State, 1000000);
            return sim;
        }

        [Test] public void AnEquipmentUpgradeReportsTheServiceTimeBeforeAndAfter()
        {
            var sim = OpenClinic();
            var before = ClinicStats.Of(sim.State);
            var predicted = ClinicStats.ServiceTicksAfterGear(sim.State, ClinicRoom.FirstAid);
            Assert.That(predicted, Is.LessThan(before.TreatmentTicks), "The panel can show the time an upgrade will give.");
            Assert.That(sim.UpgradeGear(ClinicRoom.FirstAid, 0).Success, Is.True);
            var after = ClinicStats.Of(sim.State);
            Assert.That(after.TreatmentTicks, Is.EqualTo(predicted), "The preview matches the result.");
            Assert.That(ClinicStats.ServiceTicks(sim.State, ClinicRoom.FirstAid), Is.EqualTo(after.TreatmentTicks), "The panel's current time is the same station's.");
            var waitingBefore = ClinicStats.ServiceTicks(sim.State, ClinicRoom.Waiting);
            var waitingPredicted = ClinicStats.ServiceTicksAfterGear(sim.State, ClinicRoom.Waiting);
            Assert.That(sim.UpgradeGear(ClinicRoom.Waiting, 0).Success, Is.True);
            Assert.That(ClinicStats.ServiceTicks(sim.State, ClinicRoom.Waiting), Is.EqualTo(waitingPredicted));
            Assert.That(waitingPredicted, Is.LessThanOrEqualTo(waitingBefore));
            var changes = ClinicStats.Changes(before, after);
            Assert.That(changes.Count, Is.EqualTo(1));
            Assert.That(changes[0], Is.EqualTo("Treatment " + (before.TreatmentTicks / 10d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s → "
                + (after.TreatmentTicks / 10d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s"));
        }

        [Test] public void StaffAndFeeChangesAreNamedPlainly()
        {
            var sim = OpenClinic();
            var before = ClinicStats.Of(sim.State);
            Assert.That(sim.Decorate(ClinicRoom.Reception).Success, Is.True);
            var decorated = ClinicStats.Of(sim.State);
            Assert.That(ClinicStats.Changes(before, decorated), Is.EqualTo(new[] { "Visit fee " + before.VisitFee + " → " + decorated.VisitFee + " coins" }));
            Assert.That(ClinicStats.Changes(decorated, decorated), Is.Empty, "Nothing changed, nothing to report.");
        }

        [Test] public void EveryOpenPurchaseHasAReadableNameAndTheSuggestionIsTheNearestOne()
        {
            var sim = OpenClinic();
            var options = ClinicPurchases.Options(sim);
            Assert.That(options, Is.Not.Empty);
            foreach (var option in options)
            {
                var label = ClinicPurchases.Describe(sim.State, option);
                Assert.That(label, Is.Not.Empty);
                Assert.That(label, Does.Not.Contain("Gear").And.Not.Contain("RoomUpgrade").And.Not.Contain(":"), label);
            }
            var next = ClinicPurchases.Suggest(sim);
            Assert.That(next.Cost, Is.EqualTo(options.Min(o => o.Cost)));
            Assert.That(ClinicPurchases.Describe(sim.State, next), Is.Not.Empty);
            Assert.That(ClinicPurchases.Suggest(DoctorsProgressionFixture.MaxStarter()), Is.Null, "A finished clinic has no next purchase.");
        }
    }
}
