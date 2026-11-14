using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>One tap gathers every till: counters, car park, vending machine, pharmacy and taxi stand.</summary>
    public sealed class ClinicCollectAllTests
    {
        private static ClinicSimulation Clinic(ClinicLocation location = ClinicLocation.StarterClinic)
        {
            var sim = ClinicSimulation.CreateForLocation(location);
            sim.State.Tutorial = ClinicTutorialStep.Complete;
            return sim;
        }

        [Test] public void NothingWaitingIsRefusedAndChangesNothing()
        {
            var sim = Clinic(); var wallet = sim.State.Wallet;
            var result = sim.CollectAll();
            Assert.That(result.Success, Is.False);
            Assert.That(sim.State.Wallet, Is.EqualTo(wallet));
            Assert.That(sim.DrainEvents(), Is.Empty);
        }

        [Test] public void EveryDeskAndAmenityTillIsEmptiedIntoTheWallet()
        {
            var sim = Clinic(); var state = sim.State;
            state.ReceptionDesks[0].Till = 120;
            state.Amenity(ClinicAmenity.Parking).Till = 30;
            state.Amenity(ClinicAmenity.Vending).Till = 15;
            var wallet = state.Wallet; var collected = state.TotalCollected;
            Assert.That(sim.CollectableTotal(), Is.EqualTo(165));
            var result = sim.CollectAll();
            Assert.That(result.Success, Is.True);
            Assert.That(result.Amount, Is.EqualTo(165));
            Assert.That(state.Wallet, Is.EqualTo(wallet + 165));
            Assert.That(state.TotalCollected, Is.EqualTo(collected + 165));
            Assert.That(state.ReceptionDesks.All(d => d.Till == 0), Is.True);
            Assert.That(state.Amenity(ClinicAmenity.Parking).Till, Is.EqualTo(0));
            Assert.That(state.Amenity(ClinicAmenity.Vending).Till, Is.EqualTo(0));
            Assert.That(sim.CollectableTotal(), Is.EqualTo(0));
        }

        [Test] public void SourcesCountsOnlyTillsHoldingCoins()
        {
            var sim = Clinic(); var state = sim.State;
            Assert.That(sim.CollectableSources(), Is.EqualTo(0));
            state.ReceptionDesks[0].Till = 10;
            Assert.That(sim.CollectableSources(), Is.EqualTo(1));
            state.Amenity(ClinicAmenity.Vending).Till = 5; state.Amenity(ClinicAmenity.Parking).Till = 5;
            Assert.That(sim.CollectableSources(), Is.EqualTo(3));
        }

        [Test] public void EachSourceStillAnnouncesItsOwnCollection()
        {
            var sim = Clinic(); var state = sim.State;
            state.ReceptionDesks[0].Till = 50; state.Amenity(ClinicAmenity.Vending).Till = 5;
            sim.DrainEvents();
            sim.CollectAll();
            var events = sim.DrainEvents().Where(e => e.Kind == ClinicEventKind.CashCollected).ToList();
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events.Sum(e => e.Amount), Is.EqualTo(55));
        }

        [Test] public void DoctorsClinicPharmacyAndTaxiAreIncluded()
        {
            var sim = Clinic(ClinicLocation.DoctorsClinic); var state = sim.State;
            state.PharmacyStations[0].Till = 40;
            var taxi = state.Amenity(ClinicAmenity.Taxi); if (taxi != null) taxi.Till = 25;
            var expected = 40 + (taxi != null ? 25 : 0);
            Assert.That(sim.CollectableTotal(), Is.EqualTo(expected));
            Assert.That(sim.CollectAll().Amount, Is.EqualTo(expected));
            Assert.That(state.PharmacyStations[0].Till, Is.EqualTo(0));
        }
    }
}
