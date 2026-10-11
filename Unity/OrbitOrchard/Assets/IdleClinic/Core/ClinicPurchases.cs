using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleClinic.Core
{
    public enum ClinicPurchaseKind { Renovate, RoomUpgrade, Gear, AddStation, Hire, Train, UpgradeStation, Amenity, Office }

    /// <summary>One coin purchase a clinic could make next, priced at its next level.</summary>
    public sealed class ClinicPurchaseOption
    {
        public ClinicPurchaseKind Kind { get; internal set; }
        public ClinicRoom Room { get; internal set; }
        public UpgradeTrack Track { get; internal set; }
        public ClinicStaffRole Role { get; internal set; }
        public ClinicAmenity Amenity { get; internal set; }
        public int Item { get; internal set; } = -1;
        public int StaffId { get; internal set; } = -1;
        public int StationId { get; internal set; } = -1;
        public long Cost { get; internal set; }
        /// <summary>Construction work that is available but must wait for a free builder.</summary>
        public bool WaitsForBuilder { get; internal set; }
        public override string ToString() => Kind + ":" + Room + ":" + Track + ":" + Role + ":" + Amenity + ":" + Item + ":" + StaffId + "=" + Cost;
    }

    /// <summary>Every coin purchase still open in a clinic, as a player sees them across its panels. Decor is left out
    /// under rules 5, where it costs gems. Used to keep daily goals achievable and to measure free progression.</summary>
    public static class ClinicPurchases
    {
        public static List<ClinicPurchaseOption> Options(ClinicSimulation game, bool includeWaitingForBuilder = false)
        {
            var result = new List<ClinicPurchaseOption>();
            if (game == null) return result;
            var s = game.State;
            if (s.Tutorial != ClinicTutorialStep.Complete) return result;
            var busy = game.BuildersBusy;
            foreach (var room in s.Rooms)
            {
                var kind = room.Kind;
                if (!room.Built) continue;
                if (room.Tier < ClinicRules.MaximumTier(s) && !game.IsUnderConstruction(kind) && !(game.OfficeAnchored && room.Tier >= s.OfficeLevel)
                    && (includeWaitingForBuilder || !busy))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Renovate, Room = kind, Cost = ClinicRules.RenovationCost(s, kind), WaitsForBuilder = busy });
                foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
                {
                    if (track == UpgradeTrack.Decoration && ClinicRules.Deep(s)) continue;
                    if (track == UpgradeTrack.Facilities && ClinicRules.Deep(s)) continue;
                    if (track == UpgradeTrack.Equipment && ClinicGear.Active(s, kind)) continue;
                    if (room.Level(track) < ClinicRules.TrackCap(s, kind, track))
                        Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.RoomUpgrade, Room = kind, Track = track, Cost = ClinicRules.UpgradeCost(s, kind, track) });
                }
                if (ClinicGear.Active(s, kind))
                    for (var item = 0; item < ClinicGear.ItemCount; item++)
                        if (ClinicGear.Unlocked(s, kind, item) && !ClinicGear.AtTop(s, kind, item))
                            Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Gear, Room = kind, Item = item, Cost = ClinicGear.UpgradeCost(s, kind, item) });
            }
            foreach (ClinicStaffRole role in Enum.GetValues(typeof(ClinicStaffRole)))
            {
                var max = ClinicRules.MaximumStaff(s, role);
                if (max == 0) continue;
                var room = s.Room(ClinicRules.RoomForRole(role));
                if (room == null || !room.Built) continue;
                var staffed = s.Staff.Count(x => x.Role == role);
                if (role != ClinicStaffRole.Receptionist && ClinicRules.StationCount(s, role) < ClinicRules.StationCap(s, role))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.AddStation, Role = role, Cost = ClinicRules.AddStationCost(s, role) });
                if (staffed < max && (role == ClinicStaffRole.Receptionist || staffed < ClinicRules.StationCount(s, role)))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Hire, Role = role, Cost = ClinicRules.HireCost(s, role) });
            }
            foreach (var member in s.Staff)
            {
                var room = ClinicRules.RoomForRole(member.Role);
                if (member.TrainingLevel < ClinicRules.TrainingCap(s, room))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Train, Role = member.Role, StaffId = member.Id, Cost = ClinicRules.StaffTrainingCost(s, member) });
                if (ClinicRules.StationLevel(s, member.Role, member.StationId) < ClinicRules.ComponentCap(s, room))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.UpgradeStation, Role = member.Role, StationId = member.StationId,
                        Cost = ClinicRules.StationUpgradeCost(s, member.Role, member.StationId) });
            }
            foreach (var amenity in s.Amenities)
            {
                var kind = amenity.Kind;
                if (amenity.Level < ClinicRules.AmenityCap(s, kind) && (kind == ClinicAmenity.Parking || kind == ClinicAmenity.Taxi || s.Room(ClinicRoom.Waiting).Built))
                    Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Amenity, Amenity = kind, Cost = ClinicRules.AmenityUpgradeCost(s, kind) });
            }
            if (game.OfficeAnchored && !game.OfficeUpgrading && s.OfficeLevel >= 1 && s.OfficeLevel < ClinicRules.MaximumTier(s) && s.Rooms.All(r => r.Built && r.Tier >= s.OfficeLevel)
                && (includeWaitingForBuilder ? true : s.Construction.Count == 0 && !busy))
                Add(result, new ClinicPurchaseOption { Kind = ClinicPurchaseKind.Office, Cost = game.OfficeUpgradeCost, WaitsForBuilder = busy || s.Construction.Count > 0 });
            return result;
        }

        /// <summary>The sum of every open purchase's next price: a lower bound on the coins a clinic can still spend.</summary>
        public static long NextCostsTotal(ClinicSimulation game)
        {
            long total = 0;
            foreach (var option in Options(game, true))
            {
                if (total > ClinicRules.MaximumCurrency - option.Cost) return ClinicRules.MaximumCurrency;
                total += option.Cost;
            }
            return total;
        }

        public static ClinicCommandResult Buy(ClinicSimulation game, ClinicPurchaseOption option)
        {
            switch (option.Kind)
            {
                case ClinicPurchaseKind.Renovate: return game.Renovate(option.Room);
                case ClinicPurchaseKind.RoomUpgrade: return game.Upgrade(option.Room, option.Track);
                case ClinicPurchaseKind.Gear: return game.UpgradeGear(option.Room, option.Item);
                case ClinicPurchaseKind.AddStation: return game.AddStation(option.Role);
                case ClinicPurchaseKind.Hire: return game.HireStaff(option.Role);
                case ClinicPurchaseKind.Train: return game.TrainStaff(option.StaffId);
                case ClinicPurchaseKind.UpgradeStation: return game.UpgradeStation(option.Role, option.StationId);
                case ClinicPurchaseKind.Amenity: return game.UpgradeAmenity(option.Amenity);
                case ClinicPurchaseKind.Office: return game.UpgradeOffice();
                default: return new ClinicCommandResult(false, "Unknown purchase.");
            }
        }

        /// <summary>The next step to suggest after a purchase: the nearest open improvement, by price.</summary>
        public static ClinicPurchaseOption Suggest(ClinicSimulation game)
            => Options(game, true).OrderBy(o => o.Cost).ThenBy(o => (int)o.Kind).FirstOrDefault();

        /// <summary>A purchase in the words the panels use.</summary>
        public static string Describe(ClinicState state, ClinicPurchaseOption option)
        {
            switch (option.Kind)
            {
                case ClinicPurchaseKind.Renovate: return "Renovate " + RoomName(option.Room) + " to room " + (state.Room(option.Room).Tier + 1);
                case ClinicPurchaseKind.RoomUpgrade: return "Improve " + RoomName(option.Room) + " " + (option.Track == UpgradeTrack.Equipment ? "equipment" : option.Track == UpgradeTrack.Facilities ? "facilities" : "decor");
                case ClinicPurchaseKind.Gear: return "Upgrade the " + ClinicGear.ItemName(option.Room, option.Item).ToLowerInvariant() + " in " + RoomName(option.Room);
                case ClinicPurchaseKind.AddStation: return "Add a " + StationName(option.Role);
                case ClinicPurchaseKind.Hire: return "Hire a " + RoleName(option.Role);
                case ClinicPurchaseKind.Train: return "Train a " + RoleName(option.Role);
                case ClinicPurchaseKind.UpgradeStation: return "Upgrade a " + StationName(option.Role);
                case ClinicPurchaseKind.Amenity: return "Improve the " + AmenityName(option.Amenity);
                case ClinicPurchaseKind.Office: return "Upgrade the office to clinic level " + (state.OfficeLevel + 1);
                default: return "Improve the clinic";
            }
        }

        private static string RoomName(ClinicRoom room) => room == ClinicRoom.FirstAid ? "first aid" : room == ClinicRoom.Waiting ? "the waiting room"
            : room == ClinicRoom.Consultation ? "consultations" : room == ClinicRoom.Pharmacy ? "the pharmacy" : "reception";
        private static string RoleName(ClinicStaffRole role) => role == ClinicStaffRole.Receptionist ? "receptionist" : role == ClinicStaffRole.Nurse ? "nurse"
            : role == ClinicStaffRole.Doctor ? "doctor" : "pharmacist";
        private static string StationName(ClinicStaffRole role) => role == ClinicStaffRole.Receptionist ? "check-in desk" : role == ClinicStaffRole.Nurse ? "treatment station"
            : role == ClinicStaffRole.Doctor ? "consultation room" : "pharmacy counter";
        private static string AmenityName(ClinicAmenity amenity) => amenity == ClinicAmenity.Parking ? "car park" : amenity == ClinicAmenity.Toilet ? "toilets"
            : amenity == ClinicAmenity.Vending ? "vending machine" : "taxi stand";

        private static void Add(List<ClinicPurchaseOption> list, ClinicPurchaseOption option)
        {
            if (option.Cost > 0) list.Add(option);
        }
    }
}
