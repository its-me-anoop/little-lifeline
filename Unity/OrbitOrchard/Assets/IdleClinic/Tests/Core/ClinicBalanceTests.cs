using System;
using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The balance table must reproduce every live 3.x price, time and payment exactly.</summary>
    public sealed class ClinicBalanceTests
    {
        private static readonly ClinicStaffRole[] Roles = (ClinicStaffRole[])Enum.GetValues(typeof(ClinicStaffRole));
        private static readonly ClinicRoom[] RoomKinds = (ClinicRoom[])Enum.GetValues(typeof(ClinicRoom));
        private static readonly UpgradeTrack[] Tracks = (UpgradeTrack[])Enum.GetValues(typeof(UpgradeTrack));
        private static readonly ClinicAmenity[] AmenityKinds = (ClinicAmenity[])Enum.GetValues(typeof(ClinicAmenity));

        [Test] public void EveryLiveRulesVersionUsesTheV3Table()
        {
            for (var version = 1; version <= 3; version++)
                Assert.That(ClinicBalance.For(version), Is.SameAs(ClinicBalanceTable.V3));
            Assert.Throws<ArgumentOutOfRangeException>(() => ClinicBalance.For(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ClinicBalance.For(4));
        }

        [Test] public void StarterOverloadsMatchTheFrozenFormulas()
        {
            for (var nurses = 0; nurses <= 3; nurses++) Assert.That(ClinicRules.HireNurseCost(nurses), Is.EqualTo(Frozen.HireNurseCost(nurses)));
            for (var tier = 0; tier <= 7; tier++) Assert.That(ClinicRules.RenovationSeconds(tier), Is.EqualTo(Frozen.RenovationSeconds(tier)));
            foreach (var kind in RoomKinds)
            foreach (var track in Tracks)
            {
                Assert.That(ClinicRules.UpgradeBase(kind, track), Is.EqualTo(Frozen.UpgradeBase(kind, track)), kind + " " + track);
                for (var level = 1; level <= 12; level++)
                {
                    var room = new ClinicRoomState { Kind = kind, Tier = Math.Min(6, (level + 1) / 2), EquipmentLevel = level, FacilitiesLevel = level, DecorationLevel = level };
                    Assert.That(ClinicRules.UpgradeCost(room, track), Is.EqualTo(Frozen.UpgradeCost(room, track)), kind + " " + track + " " + level);
                    Assert.That(ClinicRules.RenovationCost(room), Is.EqualTo(Frozen.RenovationCost(room)), kind + " tier " + room.Tier);
                }
            }
            foreach (var role in Roles)
            for (var level = 1; level <= 12; level++)
            {
                var staff = new ClinicStaffState { Role = role, TrainingLevel = level };
                Assert.That(ClinicRules.StaffTrainingCost(staff), Is.EqualTo(Frozen.StaffTrainingCost(staff)), role + " " + level);
            }
            foreach (var kind in AmenityKinds)
            for (var level = -1; level <= 7; level++)
                Assert.That(ClinicRules.AmenityUpgradeCost(kind, level), Is.EqualTo(Frozen.AmenityUpgradeCost(kind, level)), kind + " " + level);
        }

        [Test] public void LocationPricesTimesAndPaymentsMatchTheFrozenFormulas([Values] ClinicLocation location)
        {
            var state = Sweepable(location);
            var checks = 0;
            for (var level = 1; level <= 12; level++)
            {
                foreach (var room in state.Rooms)
                {
                    room.Tier = Math.Min(6, (level + 1) / 2);
                    room.EquipmentLevel = room.FacilitiesLevel = room.DecorationLevel = level;
                }
                foreach (var desk in state.ReceptionDesks) desk.EquipmentLevel = level;
                foreach (var role in Roles.Where(r => r != ClinicStaffRole.Receptionist))
                foreach (var station in ClinicRules.Stations(state, role)) station.EquipmentLevel = level;
                foreach (var staff in state.Staff) staff.TrainingLevel = level;
                foreach (var amenity in state.Amenities) amenity.Level = Math.Min(level - 1, 6);

                Assert.That(ClinicRules.VisitFee(state), Is.EqualTo(Frozen.VisitFee(state)), "visit fee " + level);
                Assert.That(ClinicRules.VendingTip(state), Is.EqualTo(Frozen.VendingTip(state)), "vending tip " + level);
                var parkingPatient = new ClinicPatientState { ParkingBayId = 0 };
                Assert.That(ClinicRules.ParkingFee(state), Is.EqualTo(Frozen.ParkingSurcharge(state, parkingPatient)), "parking fee " + level);
                var toiletPatient = new ClinicPatientState { UsedToilet = true };
                Assert.That(ClinicRules.VendingTipForPatient(state, toiletPatient), Is.EqualTo(Frozen.VendingTipForPatient(state, toiletPatient)), "toilet tip " + level);
                foreach (var kind in RoomKinds)
                {
                    Assert.That(ClinicRules.RenovationCost(state, kind), Is.EqualTo(Frozen.RenovationCost(state, kind)), kind + " renovation " + level);
                    Assert.That(ClinicRules.RenovationSeconds(state, kind), Is.EqualTo(Frozen.RenovationSeconds(state, kind)), kind + " renovation time " + level);
                    foreach (var track in Tracks)
                        Assert.That(ClinicRules.UpgradeCost(state, kind, track), Is.EqualTo(Frozen.UpgradeCost(state, kind, track)), kind + " " + track + " " + level);
                }
                foreach (var kind in AmenityKinds)
                    Assert.That(ClinicRules.AmenityUpgradeCost(state, kind), Is.EqualTo(Frozen.AmenityUpgradeCost(state, kind)), kind + " " + level);
                foreach (var role in Roles)
                {
                    Assert.That(ClinicRules.AddStationCost(state, role), Is.EqualTo(Frozen.AddStationCost(state, role)), role + " station " + level);
                    for (var id = 0; id < 4; id++)
                    {
                        Assert.That(ClinicRules.StationUpgradeCost(state, role, id), Is.EqualTo(Frozen.StationUpgradeCost(state, role, id)), role + " workstation " + id);
                        Assert.That(ClinicRules.StationServiceTicks(state, role, id), Is.EqualTo(Frozen.StationServiceTicks(state, role, id)), role + " service " + id);
                        Assert.That(ClinicRules.StationServiceTicks(state, role, id, 1, 2, 3), Is.EqualTo(Frozen.StationServiceTicks(state, role, id, 1, 2, 3)), role + " preview " + id);
                    }
                    foreach (var staff in state.Staff.Where(s => s.Role == role))
                        Assert.That(ClinicRules.StaffTrainingCost(state, staff), Is.EqualTo(Frozen.StaffTrainingCost(state, staff)), role + " training " + level);
                }
                for (var hired = 0; hired <= 4; hired++)
                {
                    state.Staff = Staff(state, hired);
                    foreach (var role in Roles)
                        Assert.That(ClinicRules.HireCost(state, role), Is.EqualTo(Frozen.HireCost(state, role)), role + " hire " + hired);
                }
                state.Staff = Staff(state, 4);
                foreach (var staff in state.Staff) staff.TrainingLevel = level;
                checks++;
            }
            Assert.That(checks, Is.EqualTo(12));
        }

        private static ClinicState Sweepable(ClinicLocation location)
        {
            var state = ClinicSimulation.CreateForLocation(location).State;
            foreach (var kind in RoomKinds)
                if (state.Room(kind) == null) state.Rooms.Add(new ClinicRoomState { Kind = kind });
            foreach (var room in state.Rooms) room.Built = true;
            foreach (var kind in AmenityKinds)
                if (state.Amenity(kind) == null) state.Amenities.Add(new ClinicAmenityState { Kind = kind });
            state.ReceptionDesks = Enumerable.Range(0, 4).Select(id => new ReceptionDeskState { Id = id }).ToList();
            foreach (var role in Roles.Where(r => r != ClinicStaffRole.Receptionist))
            {
                var stations = ClinicRules.Stations(state, role);
                stations.Clear();
                stations.AddRange(Enumerable.Range(0, 4).Select(id => new TreatmentStationState { Id = id }));
            }
            state.Staff = Staff(state, 4);
            return state;
        }

        private static System.Collections.Generic.List<ClinicStaffState> Staff(ClinicState state, int perRole)
            => Roles.SelectMany(role => Enumerable.Range(0, perRole).Select(id => new ClinicStaffState
                { Id = ClinicRules.StaffId(role, id), Role = role, StationId = id })).ToList();

        /// <summary>A verbatim copy of the 3.3 formulas, taken before the balance table existed. Never edit it.</summary>
        private static class Frozen
        {
            private static bool IsDoctors(ClinicState state) => state != null && state.Location == ClinicLocation.DoctorsClinic;
            private static int LocationMultiplier(ClinicState state) => IsDoctors(state) ? 2 : 1;
            private static int MaximumTier(ClinicState state) => IsDoctors(state) ? 6 : 3;
            private static int MaximumStaff(ClinicState state, ClinicStaffRole role) => !Enum.IsDefined(typeof(ClinicStaffRole), role) ? 0
                : IsDoctors(state) ? role == ClinicStaffRole.Pharmacist ? 2 : 4 : (int)role < 2 ? 2 : 0;
            private static int MaximumAmenityLevel(ClinicState state, ClinicAmenity kind) => !Enum.IsDefined(typeof(ClinicAmenity), kind) ? 0
                : kind == ClinicAmenity.Taxi && !IsDoctors(state) ? 0 : IsDoctors(state) ? 6 : 3;

            public static long HireNurseCost(int nurses) => nurses == 0 ? 50 : nurses == 1 ? 450 : 0;
            public static long UpgradeBase(ClinicRoom room, UpgradeTrack track)
            {
                if (track == UpgradeTrack.Decoration) return 40;
                if (room == ClinicRoom.Reception) return track == UpgradeTrack.Equipment ? 60 : 50;
                if (room == ClinicRoom.FirstAid) return track == UpgradeTrack.Equipment ? 80 : 60;
                return track == UpgradeTrack.Equipment ? 35 : 45;
            }
            public static long UpgradeCost(ClinicRoomState room, UpgradeTrack track)
                => room == null ? 0 : ScaleCost(UpgradeBase(room.Kind, track), 8, 5, room.Level(track) - 1);
            public static long RenovationCost(ClinicRoomState room)
                => room == null ? 0 : ScaleCost(room.Kind == ClinicRoom.Reception ? 180 : room.Kind == ClinicRoom.FirstAid ? 250 : 120, 5, 2, room.Tier - 1);
            public static int RenovationSeconds(int currentTier) => currentTier == 1 ? 60 : currentTier == 2 ? 180 : 0;
            public static long VisitFee(ClinicState state)
            {
                var percent = 100 + 15 * (state.Room(ClinicRoom.FirstAid).FacilitiesLevel - 1)
                    + 5 * state.Rooms.Where(r => r.Built).Sum(r => r.DecorationLevel - 1);
                if (IsDoctors(state)) percent += 10 * (state.Room(ClinicRoom.Consultation).FacilitiesLevel - 1) + 10 * (state.Room(ClinicRoom.Pharmacy).FacilitiesLevel - 1);
                return 50L * LocationMultiplier(state) * percent / 100;
            }
            public static long ParkingSurcharge(ClinicState state, ClinicPatientState patient)
                => patient.ParkingBayId >= 0 ? 5L * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Parking).Level : 0;
            public static long VendingTip(ClinicState state) => 5L * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Vending).Level;
            public static long VendingTipForPatient(ClinicState state, ClinicPatientState patient)
            {
                if (patient.UsedVending) return 0;
                var amount = VendingTip(state) + (patient.UsedToilet ? 2L * LocationMultiplier(state) * state.Amenity(ClinicAmenity.Toilet).Level : 0);
                return amount <= 0 ? 0 : amount;
            }
            private static int StationLevel(ClinicState state, ClinicStaffRole role, int stationId)
                => role == ClinicStaffRole.Receptionist ? state.ReceptionDesks.Find(d => d.Id == stationId)?.EquipmentLevel ?? 0
                    : ClinicRules.Stations(state, role)?.Find(s => s.Id == stationId)?.EquipmentLevel ?? 0;
            public static long StationUpgradeCost(ClinicState state, ClinicStaffRole role, int stationId)
            {
                var level = StationLevel(state, role, stationId);
                return level < 1 ? 0 : ScaleCost((role == ClinicStaffRole.Receptionist ? 90 : role == ClinicStaffRole.Nurse ? 110 : role == ClinicStaffRole.Doctor ? 150 : 120) * LocationMultiplier(state), 9, 5, level - 1);
            }
            public static long StaffTrainingCost(ClinicStaffState staff) => staff == null ? 0
                : ScaleCost(staff.Role == ClinicStaffRole.Receptionist ? 80 : 100, 9, 5, staff.TrainingLevel - 1);
            public static long AmenityUpgradeCost(ClinicAmenity kind, int currentLevel) => !Enum.IsDefined(typeof(ClinicAmenity), kind)
                || currentLevel < 0 || currentLevel >= 3 ? 0 : ScaleCost(kind == ClinicAmenity.Parking ? 220 : kind == ClinicAmenity.Toilet ? 140 : 180, 2, 1, currentLevel);
            public static int StationServiceTicks(ClinicState state, ClinicStaffRole role, int stationId,
                int equipmentLevelsAdded = 0, int trainingLevelsAdded = 0, int roomEquipmentLevelsAdded = 0)
            {
                var room = state.Room(ClinicRules.RoomForRole(role));
                var staff = state.Staff.Find(s => s.Role == role && s.StationId == stationId);
                var speed = 100 + 15 * (room.EquipmentLevel - 1 + roomEquipmentLevelsAdded)
                    + 10 * (Math.Max(1, StationLevel(state, role, stationId)) - 1 + equipmentLevelsAdded)
                    + 12 * ((staff?.TrainingLevel ?? 1) - 1 + trainingLevelsAdded);
                return ((role == ClinicStaffRole.Nurse ? 180 : role == ClinicStaffRole.Receptionist ? 140 : role == ClinicStaffRole.Doctor ? 240 : 120) * LocationMultiplier(state) * 100 + speed - 1) / speed;
            }
            public static long HireCost(ClinicState state, ClinicStaffRole role)
            {
                if (MaximumStaff(state, role) == 0) return 0;
                var count = state.Staff.Count(s => s.Role == role);
                if (count >= MaximumStaff(state, role)) return 0;
                long basis = role == ClinicStaffRole.Receptionist ? 100 : role == ClinicStaffRole.Nurse ? 50 : role == ClinicStaffRole.Doctor ? 150 : 100;
                return ScaleCost(basis * LocationMultiplier(state), role == ClinicStaffRole.Receptionist ? 3 : 9, 1, count);
            }
            public static long AddStationCost(ClinicState state, ClinicStaffRole role)
            {
                var count = ClinicRules.StationCount(state, role);
                if (role == ClinicStaffRole.Receptionist || count >= MaximumStaff(state, role)) return 0;
                return ScaleCost((role == ClinicStaffRole.Nurse ? 180 : role == ClinicStaffRole.Doctor ? 240 : 200) * LocationMultiplier(state), 5, 2, Math.Max(0, count - 1));
            }
            public static long UpgradeCost(ClinicState state, ClinicRoom kind, UpgradeTrack track)
            {
                var room = state.Room(kind);
                if (room == null) return 0;
                var basis = kind == ClinicRoom.Consultation ? (track == UpgradeTrack.Equipment ? 110 : track == UpgradeTrack.Facilities ? 90 : 40)
                    : kind == ClinicRoom.Pharmacy ? (track == UpgradeTrack.Equipment ? 95 : track == UpgradeTrack.Facilities ? 75 : 40) : UpgradeBase(kind, track);
                return ScaleCost(basis * LocationMultiplier(state), 8, 5, room.Level(track) - 1);
            }
            public static long RenovationCost(ClinicState state, ClinicRoom kind)
            {
                var room = state.Room(kind);
                var basis = kind == ClinicRoom.Reception ? 180 : kind == ClinicRoom.FirstAid ? 250 : kind == ClinicRoom.Waiting ? 120 : kind == ClinicRoom.Consultation ? 300 : 220;
                return room == null ? 0 : ScaleCost(basis * LocationMultiplier(state), 5, 2, room.Tier - 1);
            }
            public static int RenovationSeconds(ClinicState state, ClinicRoom kind) => state.Room(kind) == null || state.Room(kind).Tier >= MaximumTier(state) ? 0
                : (int)ScaleCost(60 * LocationMultiplier(state), 3, 1, state.Room(kind).Tier - 1);
            public static long StaffTrainingCost(ClinicState state, ClinicStaffState staff) => staff == null ? 0
                : ScaleCost((staff.Role == ClinicStaffRole.Receptionist ? 80 : staff.Role == ClinicStaffRole.Nurse ? 100 : staff.Role == ClinicStaffRole.Doctor ? 140 : 110) * LocationMultiplier(state), 9, 5, staff.TrainingLevel - 1);
            public static long AmenityUpgradeCost(ClinicState state, ClinicAmenity kind) => state.Amenity(kind) == null || state.Amenity(kind).Level >= MaximumAmenityLevel(state, kind) ? 0
                : ScaleCost((kind == ClinicAmenity.Parking ? 220 : kind == ClinicAmenity.Toilet ? 140 : kind == ClinicAmenity.Vending ? 180 : 260) * LocationMultiplier(state), 2, 1, state.Amenity(kind).Level);
            private static long ScaleCost(long basis, long numerator, long denominator, int exponent)
            {
                if (basis <= 0 || numerator <= 0 || denominator <= 0 || exponent < 0) return 0;
                decimal value = basis;
                for (var i = 0; i < exponent; i++)
                {
                    value = value * numerator / denominator;
                    if (value >= ClinicRules.MaximumCurrency) return ClinicRules.MaximumCurrency;
                }
                return (long)Math.Ceiling(value);
            }
        }
    }
}
