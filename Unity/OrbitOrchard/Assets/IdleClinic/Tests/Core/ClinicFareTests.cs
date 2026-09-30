using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>Pharmacy counters and taxi stands take money from patients into their own cash boxes.</summary>
    public sealed class ClinicFareTests
    {
        private static ClinicSimulation Busy(int seconds = 1800)
        {
            var game = ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic);
            Assert.That(game.State.RulesVersion, Is.EqualTo(ClinicBalance.CurrentRulesVersion));
            // Open a taxi stand so patients arrive and leave by taxi.
            game.State.Amenity(ClinicAmenity.Taxi).Level = 2;
            ClinicSimulation.TryGrantReward(game.State, 1000);
            for (var second = 0; second < seconds; second++)
            {
                game.Advance(1, false);
                Assert.That(ClinicSimulation.IsValidState(game.State), Is.True, "Invalid at second " + second);
                if (second % 20 == 0) foreach (var desk in game.State.ReceptionDesks) game.Collect(desk.Id);
            }
            return game;
        }

        [Test] public void PharmacyCountersPayIntoTheirOwnCashBoxAndCollectingMovesItToTheWallet()
        {
            var game = Busy();
            var state = game.State;
            Assert.That(state.TotalPharmacyFees, Is.GreaterThan(0), "Patients pay for their prescriptions.");
            Assert.That(state.PharmacyStations.Sum(s => s.Till), Is.GreaterThan(0));
            Assert.That(ClinicRules.PharmacyFee(state), Is.EqualTo(ClinicRules.VisitFee(state) * 40 / 100));
            var station = state.PharmacyStations.First(s => s.Till > 0);
            var wallet = state.Wallet; var till = station.Till;
            var result = game.CollectPharmacyFees(station.Id);
            Assert.That(result.Success, Is.True); Assert.That(result.Amount, Is.EqualTo(till));
            Assert.That(state.Wallet, Is.EqualTo(wallet + till)); Assert.That(station.Till, Is.Zero);
            Assert.That(game.CollectPharmacyFees(station.Id).Success, Is.False, "Nothing left to collect.");
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
        }

        [Test] public void TaxiStandsChargeAFarePerRideAndCollectingPaysTheWallet()
        {
            var game = Busy();
            var state = game.State;
            Assert.That(state.TotalTaxiFares, Is.GreaterThan(0), "Taxi passengers pay their fare.");
            Assert.That(state.TotalTaxiFares % ClinicRules.TaxiFare(state), Is.Zero, "Whole fares only.");
            var till = state.Amenity(ClinicAmenity.Taxi).Till; var wallet = state.Wallet;
            Assert.That(till, Is.GreaterThan(0));
            Assert.That(game.CollectTaxiFares().Amount, Is.EqualTo(till));
            Assert.That(state.Wallet, Is.EqualTo(wallet + till)); Assert.That(state.Amenity(ClinicAmenity.Taxi).Till, Is.Zero);
            Assert.That(game.CollectTaxiFares().Success, Is.False);
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
        }

        [Test] public void FareIncomeIsCountedAsEarnedAndOfflineAdvanceStillBalancesTheLedger()
        {
            var game = Busy(600);
            var before = game.State.TotalEarned;
            game.AdvanceOffline(4 * 3600);
            Assert.That(game.State.TotalEarned, Is.GreaterThan(before));
            Assert.That(game.TillCash, Is.EqualTo(game.State.TotalEarned - game.State.TotalCollected));
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
        }

        [Test] public void TamperedFareTotalsAreRejected()
        {
            var game = Busy(900);
            var copy = ClinicDoctorsTests.Clone(game.State);
            copy.Amenity(ClinicAmenity.Taxi).Till = copy.TotalTaxiFares + 1;
            Assert.That(ClinicSimulation.IsValidState(copy), Is.False);
            copy = ClinicDoctorsTests.Clone(game.State);
            copy.PharmacyStations[0].Till = copy.TotalPharmacyFees + 1;
            Assert.That(ClinicSimulation.IsValidState(copy), Is.False);
            copy = ClinicDoctorsTests.Clone(game.State); copy.RulesVersion = 4;
            Assert.That(ClinicSimulation.IsValidState(copy), Is.False, "Earlier rules have no fares.");
        }

        [Test] public void StarterClinicsHaveNoFaresAndTheirCashBoxesStayEmpty()
        {
            var state = ClinicSimulation.CreateNew().State;
            Assert.That(ClinicRules.PharmacyFee(state), Is.Zero); Assert.That(ClinicRules.TaxiFare(state), Is.Zero);
            state.TotalTaxiFares = 5;
            Assert.That(ClinicSimulation.IsValidState(state), Is.False);
        }
    }
}
