using System;
using System.Linq;

namespace IdleClinic.Core
{
    public static partial class ClinicRules
    {
        public const int TicksPerSecond = 10;
        public const long MaximumCurrency = 1000000000000000L;
        public const int MaximumNurses = 2;
        public const int MaximumReceptionists = 2;
        public const int MaximumRoomTier = 3;
        public const int MaximumPatients = 40;
        public const double MaximumOfflineSeconds = 8 * 60 * 60;
        /// <summary>The longest absence any offline upgrade can cover.</summary>
        public const double MaximumOfflineUpgradeSeconds = 24 * 60 * 60;
        public const int ArrivalIntervalTicks = 70;
        public const long WaitingRoomCost = 160;
        public const long TreatmentStationCost = 180;
        public const long ReceptionistCost = 300;
        public const int WaitingRoomBuildSeconds = 20;
        public const int StreetCrossingCycleTicks = 400;
        public const int StreetCrossingStartsTick = 110;
        public const int StreetCrossingEndsTick = 180;
        public const int ParkingEntryTicks = 180;
        public const int ParkingReverseTicks = 40;
        public const int ParkingGearChangeTicks = 8;
        public const int ParkingExitTravelTicks = 160;
        public const int ParkingExitTicks = ParkingReverseTicks + ParkingGearChangeTicks + ParkingExitTravelTicks;
        public const int FastestTreatmentTicks = 64;
        public const int EarliestCalledPatientCompletionTicks = 106;

        public static long TrafficTick(ClinicState state) => state.Tick - state.PausedTrafficTicks;
        public static int TrackCap(int tier) => Math.Max(1, Math.Min(6, tier)) * 2;
        public static int ComponentCap(int tier) => TrackCap(tier);
        public static long HireNurseCost(int nurses) => nurses == 0 || nurses == 1
            ? ScaleCost(V3.HireBase[(int)ClinicStaffRole.Nurse], V3.HireGrowth, nurses) : 0;
        public static long HireReceptionistCost(int receptionists) => receptionists == 1 ? ReceptionistCost : 0;
        // Starter-clinic overloads: rooms beyond the first three price as the waiting room, as they always have.
        public static long UpgradeBase(ClinicRoom room, UpgradeTrack track)
            => V3.UpgradeBase[room == ClinicRoom.Reception || room == ClinicRoom.FirstAid ? (int)room : (int)ClinicRoom.Waiting, (int)track];
        public static long UpgradeCost(ClinicRoomState room, UpgradeTrack track)
            => room == null ? 0 : ScaleCost(UpgradeBase(room.Kind, track), V3.UpgradeGrowth, room.Level(track) - 1);
        public static long RenovationCost(ClinicRoomState room)
            => room == null ? 0 : ScaleCost(V3.RenovationBase[room.Kind == ClinicRoom.Reception || room.Kind == ClinicRoom.FirstAid ? (int)room.Kind : (int)ClinicRoom.Waiting],
                V3.RenovationGrowth, room.Tier - 1);
        public static int RenovationSeconds(int currentTier) => currentTier == 1 || currentTier == 2
            ? (int)ScaleCost(V3.RenovationBaseSeconds, V3.RenovationTimeGrowth, currentTier - 1) : 0;
        // Under rules 5 the longer facilities tracks spread the same queue places and seats over more levels,
        // so the top level still fills exactly the places the building has.
        public static int UnpaidQueueCapacity(ClinicState state) => (IsDoctors(state) ? 12 : 6)
            + SpreadLevels(state, state.Room(ClinicRoom.Reception).FacilitiesLevel, IsDoctors(state) ? 11 : 5, 1);
        public static int WaitingCapacity(ClinicState state) => state.Room(ClinicRoom.Waiting).Built
            ? (IsDoctors(state) ? 8 : 4) + SpreadLevels(state, state.Room(ClinicRoom.Waiting).FacilitiesLevel, IsDoctors(state) ? 22 : 10, 2) : 2;
        /// <summary>Places added by a facilities level: a fixed step per level under earlier rules, and the same total
        /// spread evenly over the whole track under rules 5.</summary>
        private static int SpreadLevels(ClinicState state, int level, int totalPlaces, int legacyStep)
        {
            if (!Deep(state)) return legacyStep * (level - 1);
            var steps = Math.Max(1, MaximumTrackLevel(state) - 1);
            return Math.Min(totalPlaces, (int)((long)Math.Max(0, level - 1) * totalPlaces / steps));
        }
        /// <summary>No nurse treats faster than this; parking relies on it. Rules 5 raise the speed ceiling.</summary>
        public static int FastestTreatment(ClinicState state) => Deep(state) ? 29 : FastestTreatmentTicks;
        public static int EarliestCalledPatientCompletion(ClinicState state)
            => FastestTreatment(state) + EarliestCalledPatientCompletionTicks - FastestTreatmentTicks;
        public static long VisitFee(ClinicState state)
        {
            var balance = ClinicBalance.For(state);
            var percent = 100 + balance.FirstAidFacilitiesFeePercent * (state.Room(ClinicRoom.FirstAid).FacilitiesLevel - 1)
                + balance.DecorationFeePercent * state.Rooms.Where(r => r.Built).Sum(r => r.DecorationLevel - 1);
            if (IsDoctors(state)) percent += balance.ConsultationFacilitiesFeePercent * (state.Room(ClinicRoom.Consultation).FacilitiesLevel - 1)
                + balance.PharmacyFacilitiesFeePercent * (state.Room(ClinicRoom.Pharmacy).FacilitiesLevel - 1);
            return balance.VisitFeeBase * LocationMultiplier(state) * percent / 100;
        }
        /// <summary>Paid at the pharmacy counter when a prescription is collected (rules 5, doctors clinic).</summary>
        public static long PharmacyFee(ClinicState state) => IsDoctors(state) ? VisitFee(state) * ClinicBalance.For(state).PharmacyFeePercent / 100 : 0;
        public static long MaximumPharmacyFee(ClinicState state) => IsDoctors(state) ? MaximumVisitFee(state) * ClinicBalance.For(state).PharmacyFeePercent / 100 : 0;
        /// <summary>Paid to the taxi stand for each ride, to or from the clinic (rules 5, doctors clinic).</summary>
        public static long TaxiFare(ClinicState state) => IsDoctors(state) ? ClinicBalance.For(state).TaxiFarePerLevel * LocationMultiplier(state) * (state.Amenity(ClinicAmenity.Taxi)?.Level ?? 0) : 0;
        public static long MaximumTaxiFare(ClinicState state) => IsDoctors(state) ? ClinicBalanceTable.V5.TaxiFarePerLevel * LocationMultiplier(state) * MaximumAmenityLevel(state, ClinicAmenity.Taxi) : 0;
        public static long ParkingFee(ClinicState state)
            => ClinicBalance.For(state).ParkingFeePerLevel * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Parking).Level;
        public static long ParkingExitFee(ClinicState state)
            => ClinicBalance.For(state).ParkingExitFeePerLevel * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Parking).Level;
        /// <summary>Largest exit charge any car can pay here: the per-level fee at the highest car park level.</summary>
        public static long MaximumParkingExitFee(ClinicState state)
            => ClinicBalanceTable.V4.ParkingExitFeePerLevel * LocationMultiplier(state) * (IsDoctors(state) ? 6 : 3);
        /// <summary>What one parked car earns at a given car park level, under the clinic's rules.</summary>
        public static long ParkingChargePerCar(ClinicState state, int level)
        {
            var balance = ClinicBalance.For(state);
            return (balance.ParkingExitFeePerLevel > 0 ? balance.ParkingExitFeePerLevel : balance.ParkingFeePerLevel) * LocationMultiplier(state) * level;
        }
        public static double ReceptionSeconds(ClinicState state) => ReceptionTicks(state) / 10d;
        public static double TreatmentSeconds(ClinicState state) => TreatmentTicks(state) / 10d;
        public static double WaitingCallSeconds(ClinicState state) => WaitingCallTicks(state) / 10d;
        public static int ReceptionTicks(ClinicState state) => ReceptionTicks(state, 0);
        public static int TreatmentTicks(ClinicState state) => TreatmentTicks(state, 0);
        public static int WaitingCallTicks(ClinicState state, int equipmentLevelsAdded = 0) => ClinicGear.Active(state, ClinicRoom.Waiting)
            ? ClinicGear.Apply(state, ClinicRoom.Waiting, 20L * LocationMultiplier(state))
            : SpeedTicks(20 * LocationMultiplier(state), state.Room(ClinicRoom.Waiting).EquipmentLevel + equipmentLevelsAdded);
        public static int PatientAppearance(ulong seed, int patientId) => (int)((seed % 12 + (ulong)patientId * 7) % 12);
        public static int ParkingCapacity(ClinicState state) => 2 * state.Amenity(ClinicAmenity.Parking).Level;
        public static long VendingTip(ClinicState state) => ClinicBalance.For(state).VendingTipPerLevel * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Vending).Level;
        public static long VendingTipForPatient(ClinicState state, ClinicPatientState patient)
        {
            if (patient.UsedVending || state.TotalEarned < 0 || state.TotalEarned > MaximumCurrency) return 0;
            var amount = VendingTip(state) + (patient.UsedToilet ? ClinicBalance.For(state).ToiletTipPerLevel * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Toilet).Level : 0);
            if (amount <= 0 || state.TotalTips < 0 || state.TotalTips > MaximumCurrency - amount
                || state.Amenity(ClinicAmenity.Vending).Till < 0 || state.Amenity(ClinicAmenity.Vending).Till > MaximumCurrency - amount) return 0;
            var headroom = MaximumCurrency - state.TotalEarned;
            // Reception has already quoted these admissions. Optional tips cannot consume the
            // space needed to honour their pending payments.
            foreach (var admission in state.Patients.Where(p => !p.Paid && p.HasAdmissionReservation))
            {
                if (admission.Payment < 0 || admission.Payment > headroom) return 0;
                headroom -= admission.Payment;
            }
            return amount <= headroom ? amount : 0;
        }
        public static int AmenityUseTicks(ClinicState state, ClinicAmenity kind) => kind == ClinicAmenity.Toilet
            ? (6000 * LocationMultiplier(state) + 100 + 25 * (state.Amenity(kind).Level - 1) - 1) / (100 + 25 * (state.Amenity(kind).Level - 1)) : 30 * LocationMultiplier(state);
        public static int StationLevel(ClinicState state, ClinicStaffRole role, int stationId)
            => role == ClinicStaffRole.Receptionist ? state.ReceptionDesks.Find(d => d.Id == stationId)?.EquipmentLevel ?? 0
                : Stations(state, role)?.Find(s => s.Id == stationId)?.EquipmentLevel ?? 0;
        public static long StationUpgradeCost(ClinicState state, ClinicStaffRole role, int stationId)
        {
            var level = StationLevel(state, role, stationId);
            var balance = ClinicBalance.For(state);
            return level < 1 ? 0 : ScaleCost(balance.StationUpgradeBase[(int)role] * LocationMultiplier(state), IsDoctors(state) ? balance.DoctorsStationUpgradeGrowth : balance.StationUpgradeGrowth, level - 1);
        }
        // Starter-clinic overloads: every role but reception trains at the nurse price, and the taxi
        // stand prices as vending, exactly as before the balance table existed.
        public static long StaffTrainingCost(ClinicStaffState staff) => staff == null ? 0
            : ScaleCost(V3.TrainingBase[staff.Role == ClinicStaffRole.Receptionist ? (int)ClinicStaffRole.Receptionist : (int)ClinicStaffRole.Nurse],
                V3.TrainingGrowth, staff.TrainingLevel - 1);
        public static long AmenityUpgradeCost(ClinicAmenity kind, int currentLevel) => !Enum.IsDefined(typeof(ClinicAmenity), kind)
            || currentLevel < 0 || currentLevel >= 3 ? 0
            : ScaleCost(V3.AmenityBase[kind == ClinicAmenity.Taxi ? (int)ClinicAmenity.Vending : (int)kind], V3.AmenityGrowth, currentLevel);
        public static int ReceptionTicks(ClinicState state, int deskId) => StationServiceTicks(state, ClinicStaffRole.Receptionist, deskId);
        public static int TreatmentTicks(ClinicState state, int stationId) => StationServiceTicks(state, ClinicStaffRole.Nurse, stationId);
        public static int StationServiceTicks(ClinicState state, ClinicStaffRole role, int stationId,
            int equipmentLevelsAdded = 0, int trainingLevelsAdded = 0, int roomEquipmentLevelsAdded = 0, int gearStepsAdded = 0)
        {
            var balance = ClinicBalance.For(state);
            var room = state.Room(RoomForRole(role));
            var staff = state.Staff.Find(s => s.Role == role && s.StationId == stationId);
            // Equipment pieces (rules 5) take a share of the remaining time off; the older equipment level adds to speed instead.
            var kind = RoomForRole(role);
            var gear = ClinicGear.Active(state, kind);
            var speed = 100 + (gear ? 0 : balance.RoomEquipmentSpeedPercent * (room.EquipmentLevel - 1 + roomEquipmentLevelsAdded))
                + balance.StationEquipmentSpeedPercent * (Math.Max(1, StationLevel(state, role, stationId)) - 1 + equipmentLevelsAdded)
                + balance.TrainingSpeedPercent * ((staff?.TrainingLevel ?? 1) - 1 + trainingLevelsAdded);
            var ticks = (int)((balance.ServiceBaseTicks[(int)role] * LocationMultiplier(state) * 100L + speed - 1) / speed);
            if (gear) ticks = ClinicGear.Apply(state, kind, ticks, gearStepsAdded);
            return role == ClinicStaffRole.Nurse && Deep(state) ? Math.Max(FastestTreatment(state), ticks) : ticks;
        }
        public static string ParkingPatientAnchor(int id) => "parking.bay." + id + ".patient";
        public static string AmenityPatientAnchor(ClinicAmenity kind) => kind == ClinicAmenity.Toilet ? "waiting.toilet.patient" : "waiting.vending.patient";
        private static int SpeedTicks(int baseTicks, int level) => (baseTicks * 100 + (100 + 15 * (level - 1)) - 1) / (100 + 15 * (level - 1));
        private static ClinicBalanceTable V3 => ClinicBalanceTable.V3;
        private static long ScaleCost(long basis, ClinicGrowth growth, int exponent) => ScaleCost(basis, growth.Numerator, growth.Denominator, exponent);
        internal static long Compound(long basis, ClinicGrowth growth, int exponent) => ScaleCost(basis, growth, exponent);
        private static long ScaleCost(long basis, long numerator, long denominator, int exponent)
        {
            if (basis <= 0 || numerator <= 0 || denominator <= 0 || exponent < 0) return 0;
            decimal value = basis;
            for (var i = 0; i < exponent; i++)
            {
                value = value * numerator / denominator;
                if (value >= MaximumCurrency) return MaximumCurrency;
            }
            return (long)Math.Ceiling(value);
        }
        public static string DeskPatientAnchor(int id) => "reception.desk." + id + ".patient";
        public static string DeskStaffAnchor(int id) => "reception.desk." + id + ".staff";
        public static string DeskCashAnchor(int id) => "reception.desk." + id + ".cash";
        public static string TreatmentPatientAnchor(int id) => "firstaid.station." + id + ".patient";
        public static string TreatmentStaffAnchor(int id) => "firstaid.station." + id + ".staff";
        public static string QueueAnchor(int id) => "reception.queue." + id;
        public static string WaitingAnchor(bool built, int id) => (built ? "waiting.seat." : "firstaid.standing.") + id;
    }
}
