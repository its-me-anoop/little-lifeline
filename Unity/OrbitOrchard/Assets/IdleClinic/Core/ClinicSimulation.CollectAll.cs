using System.Linq;

namespace IdleClinic.Core
{
    public sealed partial class ClinicSimulation
    {
        /// <summary>Coins waiting in every till of this clinic: reception counters, car park, vending machine, pharmacy and taxi stand.</summary>
        public long CollectableTotal()
        {
            var total = State.ReceptionDesks.Sum(d => d.Till);
            foreach (var kind in new[] { ClinicAmenity.Parking, ClinicAmenity.Vending, ClinicAmenity.Taxi }) total += State.Amenity(kind)?.Till ?? 0;
            total += State.PharmacyStations?.Sum(s => s.Till) ?? 0;
            return total;
        }

        /// <summary>How many separate tills hold coins, so "collect all" is only offered when it saves a tap.</summary>
        public int CollectableSources()
        {
            var count = State.ReceptionDesks.Count(d => d.Till > 0);
            foreach (var kind in new[] { ClinicAmenity.Parking, ClinicAmenity.Vending, ClinicAmenity.Taxi }) if ((State.Amenity(kind)?.Till ?? 0) > 0) count++;
            return count + (State.PharmacyStations?.Count(s => s.Till > 0) ?? 0);
        }

        /// <summary>Collect from every till at once. Each source still announces its own collection, so the world shows coins
        /// leaving every counter. Refused when nothing is waiting.</summary>
        public ClinicCommandResult CollectAll()
        {
            if (CollectableTotal() <= 0) return No("Nothing is waiting to be collected.");
            long amount = 0;
            foreach (var desk in State.ReceptionDesks.Where(d => d.Till > 0).ToList()) amount += Collect(desk.Id).Amount;
            if ((State.Amenity(ClinicAmenity.Parking)?.Till ?? 0) > 0) amount += CollectParkingFees().Amount;
            if ((State.Amenity(ClinicAmenity.Vending)?.Till ?? 0) > 0) amount += CollectVendingTips().Amount;
            if ((State.Amenity(ClinicAmenity.Taxi)?.Till ?? 0) > 0) amount += CollectTaxiFares().Amount;
            foreach (var station in (State.PharmacyStations ?? new System.Collections.Generic.List<TreatmentStationState>()).Where(s => s.Till > 0).ToList()) amount += CollectPharmacyFees(station.Id).Amount;
            return amount > 0 ? Yes("Everything collected.", amount: amount) : No("The clinic wallet is full.");
        }
    }
}
