using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleClinic.Core
{
    public static partial class ClinicRules
    {
        public const long DoctorsClinicUnlockCost = 100000;
        public const int TaxiArrivalTicks = 160;
        public const int TaxiDropOffTicks = 30;
        public const int TaxiPickupTicks = 30;
        public const int TaxiDepartureTicks = 160;
        public static bool IsDoctors(ClinicState state) => state != null && state.Location == ClinicLocation.DoctorsClinic;
        public static int LocationMultiplier(ClinicState state) => IsDoctors(state) ? 2 : 1;
        public static int MaximumTier(ClinicState state) => Deep(state) ? ClinicBalance.For(state).StarterMaximumTier * LocationMultiplier(state) : IsDoctors(state) ? 6 : 3;
        /// <summary>Rules 5 and later: many more levels, and decor bought with gems.</summary>
        public static bool Deep(ClinicState state) => ClinicBalance.For(state).DeepProgression;
        /// <summary>Highest equipment, facilities and workstation level anywhere in this clinic.</summary>
        public static int MaximumTrackLevel(ClinicState state) => Deep(state) ? ClinicBalance.For(state).StarterMaximumTrackLevel * LocationMultiplier(state) : TrackCap(MaximumTier(state));
        public static int MaximumTrainingLevel(ClinicState state) => Deep(state) ? ClinicBalance.For(state).StarterMaximumTrainingLevel * LocationMultiplier(state) : TrackCap(MaximumTier(state));
        public static int MaximumDecorationLevel(ClinicState state) => Deep(state) ? ClinicBalance.For(state).StarterMaximumDecorationLevel * LocationMultiplier(state) : TrackCap(MaximumTier(state));
        /// <summary>The level a track may reach at this room size. Under rules 5 each size opens an even share of the
        /// track, so the first size already allows one improvement and the last size opens the top level.</summary>
        public static int TrackCap(ClinicState state, int tier) => Deep(state) ? ProgressCap(MaximumTrackLevel(state), tier, MaximumTier(state)) : TrackCap(tier);
        public static int TrainingCap(ClinicState state, ClinicRoom kind) => state.Room(kind) == null ? 0
            : Deep(state) ? ProgressCap(MaximumTrainingLevel(state), state.Room(kind).Tier, MaximumTier(state)) : TrackCap(state.Room(kind).Tier);
        /// <summary>Decor is independent of room size under rules 5: any level up to the maximum, bought with gems.</summary>
        public static int DecorationCap(ClinicState state, ClinicRoom kind) => state.Room(kind) == null ? 0
            : Deep(state) ? MaximumDecorationLevel(state) : ComponentCap(state, kind);
        public static int TrackCap(ClinicState state, ClinicRoom kind, UpgradeTrack track) => track == UpgradeTrack.Decoration ? DecorationCap(state, kind) : ComponentCap(state, kind);
        /// <summary>Levels a saved clinic may own. Rules 5 let owned levels exceed the current room size's cap, so
        /// clinics that move to the new rules keep everything; the cap only limits the next purchase.</summary>
        public static int OwnedLevelLimit(ClinicState state, int tier, UpgradeTrack track) => Deep(state)
            ? track == UpgradeTrack.Decoration ? MaximumDecorationLevel(state) : MaximumTrackLevel(state) : TrackCap(tier);
        public static int OwnedTrainingLimit(ClinicState state, int tier) => Deep(state) ? MaximumTrainingLevel(state) : TrackCap(tier);
        /// <summary>Rules any saved clinic may carry.</summary>
        public static bool KnownRules(int rulesVersion) => rulesVersion >= 3 && rulesVersion <= ClinicBalance.CurrentRulesVersion;
        /// <summary>The smallest room size whose cap allows this track (or training) level; zero if none does.</summary>
        public static int TierUnlocking(ClinicState state, int level, bool training = false)
        {
            for (var tier = 1; tier <= MaximumTier(state); tier++)
                if ((training ? (Deep(state) ? ProgressCap(MaximumTrainingLevel(state), tier, MaximumTier(state)) : TrackCap(tier)) : TrackCap(state, tier)) >= level) return tier;
            return 0;
        }
        private static int ProgressCap(int maximum, int tier, int maximumTier)
            => Math.Max(1, Math.Min(maximum, 1 + (Math.Max(1, tier) * (maximum - 1) + maximumTier - 1) / Math.Max(1, maximumTier)));
        public static int MaximumStaff(ClinicState state, ClinicStaffRole role) => !Enum.IsDefined(typeof(ClinicStaffRole), role) ? 0
            : IsDoctors(state) ? role == ClinicStaffRole.Pharmacist ? 2 : 4 : (int)role < 2 ? 2 : 0;
        public static int MaximumPatientCount(ClinicState state) => IsDoctors(state) ? 80 : MaximumPatients;
        public static int ComponentCap(ClinicState state, ClinicRoom kind) => state.Room(kind) == null ? 0 : TrackCap(state, state.Room(kind).Tier);
        public static int MaximumAmenityLevel(ClinicState state, ClinicAmenity kind) => !Enum.IsDefined(typeof(ClinicAmenity), kind) ? 0
            : kind == ClinicAmenity.Taxi && !IsDoctors(state) ? 0 : IsDoctors(state) ? 6 : 3;
        public static int AmenityCap(ClinicState state, ClinicAmenity kind) => kind == ClinicAmenity.Parking || kind == ClinicAmenity.Taxi
            ? MaximumAmenityLevel(state, kind) : Math.Min(MaximumAmenityLevel(state, kind), state.Room(ClinicRoom.Waiting).Tier);
        public static int ToiletCubicleCount(ClinicState state) => state.Amenity(ClinicAmenity.Toilet).Level == 0 ? 0 : IsDoctors(state) ? 2 : 1;
        public static int TaxiDockCount(ClinicState state) => IsDoctors(state) && state.Amenity(ClinicAmenity.Taxi).Level > 0 ? 2 : 0;
        public static int TaxiTravelTicks(ClinicState state) => SpeedTicks(TaxiArrivalTicks, state.Amenity(ClinicAmenity.Taxi).Level);
        public static ClinicRoom RoomForRole(ClinicStaffRole role) => role == ClinicStaffRole.Nurse ? ClinicRoom.FirstAid
            : role == ClinicStaffRole.Doctor ? ClinicRoom.Consultation : role == ClinicStaffRole.Pharmacist ? ClinicRoom.Pharmacy : ClinicRoom.Reception;
        public static int StaffId(ClinicStaffRole role, int station) => (int)role * 100 + station;
        public static List<TreatmentStationState> Stations(ClinicState state, ClinicStaffRole role) => role == ClinicStaffRole.Nurse ? state.TreatmentStations
            : role == ClinicStaffRole.Doctor ? state.ConsultationStations : role == ClinicStaffRole.Pharmacist ? state.PharmacyStations : null;
        public static int StationCount(ClinicState state, ClinicStaffRole role) => role == ClinicStaffRole.Receptionist ? state.ReceptionDesks.Count : Stations(state, role)?.Count ?? 0;
        public static int StationCap(ClinicState state, ClinicStaffRole role) => role == ClinicStaffRole.Receptionist ? MaximumStaff(state, role)
            : Math.Min(MaximumStaff(state, role), state.Room(RoomForRole(role))?.Tier ?? 0);
        public static long HireCost(ClinicState state, ClinicStaffRole role)
        {
            if (MaximumStaff(state, role) == 0) return 0;
            var count = state.Staff.Count(s => s.Role == role);
            if (count >= MaximumStaff(state, role)) return 0;
            var balance = ClinicBalance.For(state);
            return ScaleCost(balance.HireBase[(int)role] * LocationMultiplier(state),
                role == ClinicStaffRole.Receptionist ? balance.ReceptionistHireGrowth : balance.HireGrowth, count);
        }
        public static long AddStationCost(ClinicState state, ClinicStaffRole role)
        {
            var count = StationCount(state, role);
            if (role == ClinicStaffRole.Receptionist || count >= MaximumStaff(state, role)) return 0;
            var balance = ClinicBalance.For(state);
            return ScaleCost(balance.AddStationBase[(int)role] * LocationMultiplier(state), balance.AddStationGrowth, Math.Max(0, count - 1));
        }
        public static long UpgradeCost(ClinicState state, ClinicRoom kind, UpgradeTrack track)
        {
            var room = state.Room(kind);
            if (room == null) return 0;
            var balance = ClinicBalance.For(state);
            if (balance.DeepProgression && track == UpgradeTrack.Decoration) return 0;
            return ScaleCost(balance.UpgradeBase[(int)kind, (int)track] * LocationMultiplier(state), IsDoctors(state) ? balance.DoctorsUpgradeGrowth : balance.UpgradeGrowth, room.Level(track) - 1);
        }
        public static long RenovationCost(ClinicState state, ClinicRoom kind)
        {
            var room = state.Room(kind);
            var balance = ClinicBalance.For(state);
            return room == null ? 0 : ScaleCost(balance.RenovationBase[(int)kind] * LocationMultiplier(state), IsDoctors(state) ? balance.DoctorsRenovationGrowth : balance.RenovationGrowth, room.Tier - 1);
        }
        public static int RenovationSeconds(ClinicState state, ClinicRoom kind)
        {
            var room = state.Room(kind);
            if (room == null || room.Tier >= MaximumTier(state)) return 0;
            var balance = ClinicBalance.For(state);
            return (int)Math.Min(balance.MaximumConstructionSeconds, ScaleCost(balance.RenovationBaseSeconds * LocationMultiplier(state),
                IsDoctors(state) ? balance.DoctorsRenovationTimeGrowth : balance.RenovationTimeGrowth, room.Tier - 1));
        }
        public static long StaffTrainingCost(ClinicState state, ClinicStaffState staff) => staff == null ? 0
            : ScaleCost(ClinicBalance.For(state).TrainingBase[(int)staff.Role] * LocationMultiplier(state),
                IsDoctors(state) ? ClinicBalance.For(state).DoctorsTrainingGrowth : ClinicBalance.For(state).TrainingGrowth, staff.TrainingLevel - 1);
        /// <summary>Gems for the next decor level of a room (rules 5); zero at the top level or under earlier rules.</summary>
        public static long DecorationGemCost(ClinicState state, ClinicRoom kind)
        {
            var room = state.Room(kind);
            var balance = ClinicBalance.For(state);
            if (room == null || !room.Built || !balance.DeepProgression || room.DecorationLevel >= MaximumDecorationLevel(state)) return 0;
            return ScaleCost(balance.DecorationGemBase * LocationMultiplier(state), balance.DecorationGemGrowth, room.DecorationLevel - 1);
        }
        public static long AmenityUpgradeCost(ClinicState state, ClinicAmenity kind) => state.Amenity(kind) == null || state.Amenity(kind).Level >= MaximumAmenityLevel(state, kind) ? 0
            : ScaleCost(ClinicBalance.For(state).AmenityBase[(int)kind] * LocationMultiplier(state), ClinicBalance.For(state).AmenityGrowth, state.Amenity(kind).Level);
        public static string StationPatientAnchor(ClinicStaffRole role, int id) => role == ClinicStaffRole.Receptionist ? DeskPatientAnchor(id)
            : role == ClinicStaffRole.Nurse ? TreatmentPatientAnchor(id) : (role == ClinicStaffRole.Doctor ? "consultation.station." : "pharmacy.station.") + id + ".patient";
        public static string StationStaffAnchor(ClinicStaffRole role, int id) => role == ClinicStaffRole.Receptionist ? DeskStaffAnchor(id)
            : role == ClinicStaffRole.Nurse ? TreatmentStaffAnchor(id) : (role == ClinicStaffRole.Doctor ? "consultation.station." : "pharmacy.station.") + id + ".staff";
        public static string ToiletPatientAnchor(ClinicState state, int id) => IsDoctors(state) ? "waiting.toilet." + id + ".patient" : AmenityPatientAnchor(ClinicAmenity.Toilet);
        public static string TaxiPatientAnchor(int id) => "taxi.dock." + id + ".patient";
        public const int TaxiWaitingCapacity = 8;
        public const int TaxiBookingCapacity = 6;
        public static string TaxiWaitingAnchor(int id) => "taxi.waiting." + id + ".patient";
        public static string RoomPlotAnchor(ClinicRoom room) => room == ClinicRoom.FirstAid ? "firstaid.plot" : room.ToString().ToLowerInvariant() + ".plot";
        /// <summary>How many coins the tills hold while nobody is there (rules 4): about this many visits' fees.
        /// Zero means no coin limit, as under earlier rules.</summary>
        public static long OfflineCoinCap(ClinicState state, double multiplier = 1)
        {
            var visits = ClinicBalance.For(state).OfflineCapVisits;
            return visits <= 0 ? 0 : (long)Math.Min(MaximumCurrency, Math.Ceiling(visits * VisitFee(state) * Math.Max(1, multiplier)));
        }
        /// <summary>The highest visit fee this clinic's rules allow, never below the long-standing ceilings.</summary>
        public static long MaximumVisitFee(ClinicState state)
        {
            long legacy = IsDoctors(state) ? 820 : 140;
            var balance = ClinicBalance.For(state);
            if (!balance.DeepProgression) return legacy;
            long track = MaximumTrackLevel(state) - 1, decor = MaximumDecorationLevel(state) - 1;
            long percent = 100 + balance.FirstAidFacilitiesFeePercent * track + balance.DecorationFeePercent * decor * (IsDoctors(state) ? 5 : 3)
                + (IsDoctors(state) ? (balance.ConsultationFacilitiesFeePercent + balance.PharmacyFacilitiesFeePercent) * track : 0)
                + (OffersServiceRooms(state) ? balance.OfficeFeePercent : 0);
            return Math.Max(legacy, balance.VisitFeeBase * LocationMultiplier(state) * percent / 100);
        }
        public static List<string> StarterCompletion(ClinicState state)
        {
            var unmet = new List<string>();
            if (state == null || state.Location != ClinicLocation.StarterClinic) { unmet.Add("Complete the starter clinic."); return unmet; }
            var deep = Deep(state);
            int tier = MaximumTier(state), track = MaximumTrackLevel(state), training = MaximumTrainingLevel(state);
            if (state.Tutorial != ClinicTutorialStep.Complete) unmet.Add("Complete the first treatment.");
            if (state.Construction.Count != 0) unmet.Add("Finish all construction.");
            foreach (ClinicRoom kind in new[] { ClinicRoom.Reception, ClinicRoom.FirstAid, ClinicRoom.Waiting })
            {
                var room = state.Room(kind);
                if (room == null || !room.Built) { unmet.Add("Build " + kind + "."); continue; }
                if (room.Tier != tier) unmet.Add("Renovate " + kind + " to tier " + tier + ".");
                foreach (UpgradeTrack t in Enum.GetValues(typeof(UpgradeTrack)))
                    // Decor is optional under rules 5: it never gates the next clinic.
                    if ((!deep || t != UpgradeTrack.Decoration) && room.Level(t) < track) unmet.Add("Maximise " + kind + " " + t + ".");
            }
            foreach (var role in new[] { ClinicStaffRole.Receptionist, ClinicStaffRole.Nurse })
            {
                if (StationCount(state, role) != 2) unmet.Add("Build both " + role + " stations.");
                if (state.Staff.Count(s => s.Role == role) != 2) unmet.Add("Hire both " + role + " staff.");
                for (var id = 0; id < 2; id++)
                {
                    if (StationLevel(state, role, id) < track) unmet.Add("Maximise " + role + " workstation " + (id + 1) + ".");
                    if ((state.Staff.Find(s => s.Role == role && s.StationId == id)?.TrainingLevel ?? 0) < training) unmet.Add("Fully train " + role + " " + (id + 1) + ".");
                }
            }
            foreach (var kind in new[] { ClinicAmenity.Parking, ClinicAmenity.Toilet, ClinicAmenity.Vending })
                if (state.Amenity(kind)?.Level != MaximumAmenityLevel(state, kind)) unmet.Add("Maximise " + kind + ".");
            return unmet;
        }
    }
}