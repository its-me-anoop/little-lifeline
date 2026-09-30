using System.Linq;

namespace IdleClinic.Core
{
    /// <summary>Pharmacy counters and taxi stands take money from patients too (rules 5, doctors clinic). Like the car park, each
    /// pays into its own cash box, counted as earned now and as collected when the player taps it.</summary>
    public sealed partial class ClinicSimulation
    {
        private void ChargePharmacyFee(ClinicPatientState patient)
        {
            var fee = ClinicRules.PharmacyFee(State);
            var station = ClinicRules.Stations(State, ClinicStaffRole.Pharmacist)?.Find(s => s.Id == patient.PharmacyStationId);
            if (fee <= 0 || station == null || State.TotalEarned > MaximumMoney - fee || station.Till > MaximumMoney - fee || State.TotalPharmacyFees > MaximumMoney - fee) return;
            station.Till += fee; State.TotalPharmacyFees += fee; State.TotalEarned += fee;
            Emit(ClinicEventKind.PharmacyFeePaid, ClinicRoom.Pharmacy, patient.Id, deskId: station.Id, amount: fee, source: ClinicRules.StationPatientAnchor(ClinicStaffRole.Pharmacist, station.Id));
        }

        private void ChargeTaxiFare(ClinicPatientState patient)
        {
            var fare = ClinicRules.TaxiFare(State); var stand = State.Amenity(ClinicAmenity.Taxi);
            if (fare <= 0 || stand == null || State.TotalEarned > MaximumMoney - fare || stand.Till > MaximumMoney - fare || State.TotalTaxiFares > MaximumMoney - fare) return;
            stand.Till += fare; State.TotalTaxiFares += fare; State.TotalEarned += fare;
            Emit(ClinicEventKind.TaxiFarePaid, ClinicRoom.Reception, patient.Id, amount: fare, source: "taxi.stand", amenity: ClinicAmenity.Taxi);
        }

        public ClinicCommandResult CollectPharmacyFees(int stationId)
        {
            var station = ClinicRules.Stations(State, ClinicStaffRole.Pharmacist)?.Find(s => s.Id == stationId);
            var amount = station?.Till ?? 0;
            if (amount <= 0) return No("Pharmacy fees collect here as prescriptions are handed over.");
            if (State.Wallet > MaximumMoney - amount) return No("The clinic wallet is full.");
            State.Wallet += amount; State.TotalCollected += amount; station.Till = 0;
            Emit(ClinicEventKind.CashCollected, ClinicRoom.Pharmacy, deskId: stationId, amount: amount, source: "pharmacy.cash");
            return Yes("Pharmacy fees collected.", amount: amount);
        }

        public ClinicCommandResult CollectTaxiFares()
        {
            var stand = State.Amenity(ClinicAmenity.Taxi); var amount = stand?.Till ?? 0;
            if (amount <= 0) return No("Taxi fares collect here as passengers ride.");
            if (State.Wallet > MaximumMoney - amount) return No("The clinic wallet is full.");
            State.Wallet += amount; State.TotalCollected += amount; stand.Till = 0;
            Emit(ClinicEventKind.CashCollected, ClinicRoom.Reception, amount: amount, source: "taxi.cash", amenity: ClinicAmenity.Taxi);
            return Yes("Taxi fares collected.", amount: amount);
        }
    
        private static bool ValidFareLedger(ClinicState state)
        {
            if (!MoneyRange(state.TotalPharmacyFees) || !MoneyRange(state.TotalTaxiFares) || state.PharmacyStations == null || state.Amenities == null) return false;
            var taxi = state.Amenity(ClinicAmenity.Taxi)?.Till ?? 0;
            var pharmacy = state.PharmacyStations.Where(s => s != null).Sum(s => s.Till);
            if (state.RulesVersion < 5) return state.TotalPharmacyFees == 0 && state.TotalTaxiFares == 0 && taxi == 0 && pharmacy == 0;
            return taxi >= 0 && taxi <= state.TotalTaxiFares && pharmacy >= 0 && pharmacy <= state.TotalPharmacyFees
                && (decimal)state.TotalPharmacyFees <= (decimal)ClinicRules.MaximumPharmacyFee(state) * state.NextPatientId
                && (decimal)state.TotalTaxiFares <= 2m * ClinicRules.MaximumTaxiFare(state) * state.NextPatientId;
        }
    }
}
