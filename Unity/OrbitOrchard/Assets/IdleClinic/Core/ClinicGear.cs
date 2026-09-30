using System;
using System.Collections.Generic;

namespace IdleClinic.Core
{
    /// <summary>Room equipment (rules 5). Every room has twenty pieces that arrive as the room is renovated, and each piece has
    /// ten versions, from basic to advanced, bought one at a time. Each upgrade removes a tenth of the time the room still has
    /// to lose (its service time above a floor of six per cent of the base time), so the first upgrades are the biggest and the
    /// floor is reached only after most of the twenty pieces are done. Owned equipment levels convert to the same or better
    /// speed, so no clinic slows down.</summary>
    public static class ClinicGear
    {
        public const int ItemCount = 20;
        public const int MaximumVersion = 10;
        public const int StepsPerItem = MaximumVersion - 1;
        public const int TotalSteps = ItemCount * StepsPerItem;
        private const long One = 1000000;

        /// <summary>The fastest any service gets, as a percentage of its base time.</summary>
        public const int FloorPercent = 6;

        // Millionths of the removable time still left after n upgrades: 0.9 ^ n, from exact decimal arithmetic.
        private static readonly long[] Remaining = BuildRemaining();
        private static long[] BuildRemaining()
        {
            var table = new long[TotalSteps + 1];
            var value = 1m;
            for (var i = 0; i <= TotalSteps; i++) { table[i] = (long)Math.Round(value * One, MidpointRounding.AwayFromZero); value *= 0.9m; }
            return table;
        }

        /// <summary>A room's base service time in ticks, before any speed-up.</summary>
        public static long BaseTicks(ClinicState state, ClinicRoom kind)
            => kind == ClinicRoom.Waiting ? 20L * ClinicRules.LocationMultiplier(state)
                : (long)ClinicBalance.For(state).ServiceBaseTicks[(int)RoleOf(kind)] * ClinicRules.LocationMultiplier(state);
        private static ClinicStaffRole RoleOf(ClinicRoom kind) => kind == ClinicRoom.Reception ? ClinicStaffRole.Receptionist : kind == ClinicRoom.FirstAid ? ClinicStaffRole.Nurse
            : kind == ClinicRoom.Consultation ? ClinicStaffRole.Doctor : ClinicStaffRole.Pharmacist;
        public static long FloorTicks(ClinicState state, ClinicRoom kind) => (BaseTicks(state, kind) * FloorPercent + 99) / 100;

        /// <summary>A service time after this room's equipment: each upgrade removes a tenth of what can still be removed.</summary>
        public static int Apply(ClinicState state, ClinicRoom kind, long ticks, int stepsAdded = 0)
        {
            var floor = FloorTicks(state, kind);
            if (ticks <= floor) return (int)ticks;
            var steps = Math.Max(0, Math.Min(TotalSteps, Steps(state, kind) + stepsAdded));
            return (int)(floor + ((ticks - floor) * Remaining[steps] + One - 1) / One);
        }

        public static bool Allowed(ClinicState state, ClinicRoom kind)
            => kind == ClinicRoom.Reception || kind == ClinicRoom.FirstAid || kind == ClinicRoom.Waiting || ClinicRules.IsDoctors(state);
        /// <summary>Whether this room prices its equipment per piece.</summary>
        public static bool Active(ClinicState state, ClinicRoom kind)
        {
            if (state == null || !ClinicRules.Deep(state) || !Allowed(state, kind)) return false;
            var room = state.Room(kind);
            return room != null && room.Built;
        }
        private static bool Seeded(ClinicRoomState room) => room.GearLevels != null && room.GearLevels.Count == ItemCount;

        /// <summary>The fewest upgrades that leave a room at least as fast as an old equipment level of <paramref name="equipmentLevel"/>.</summary>
        private static int ConvertedSteps(ClinicState state, ClinicRoom kind, int equipmentLevel)
        {
            if (equipmentLevel <= 1) return 0;
            var basis = BaseTicks(state, kind); var floor = FloorTicks(state, kind);
            var extra = 100 + ClinicBalance.For(state).RoomEquipmentSpeedPercent * (equipmentLevel - 1);
            // The old time at this level, in millionths of a tick; we need floor + (base - floor) * remaining <= that.
            var oldMillionths = basis * 100 * One / extra;
            for (var steps = 0; steps <= TotalSteps; steps++)
                if (floor * One + (basis - floor) * Remaining[steps] <= oldMillionths) return steps;
            return TotalSteps;
        }
        private static int VersionOfStep(int steps, int item) => 1 + Math.Max(0, Math.Min(StepsPerItem, steps - item * StepsPerItem));

        /// <summary>Give a room its item list on the first purchase, converting what it already owns. Idempotent.</summary>
        public static void EnsureSeeded(ClinicState state, ClinicRoom kind)
        {
            if (!Active(state, kind)) return;
            var room = state.Room(kind);
            if (Seeded(room)) return;
            var steps = ConvertedSteps(state, kind, room.EquipmentLevel);
            var levels = new List<int>(ItemCount);
            for (var item = 0; item < ItemCount; item++) levels.Add(VersionOfStep(steps, item));
            room.GearLevels = levels;
        }

        public static int Version(ClinicState state, ClinicRoom kind, int item)
        {
            if (!Active(state, kind) || !ValidItem(item)) return 1;
            var room = state.Room(kind);
            return Seeded(room) ? room.GearLevels[item] : VersionOfStep(ConvertedSteps(state, kind, room.EquipmentLevel), item);
        }
        /// <summary>Upgrades owned across the room's twenty pieces (each version above the first is one).</summary>
        public static int Steps(ClinicState state, ClinicRoom kind)
        {
            if (!Active(state, kind)) return 0;
            var room = state.Room(kind);
            return Seeded(room) ? Steps(room) : ConvertedSteps(state, kind, room.EquipmentLevel);
        }
        public static int Steps(ClinicRoomState room)
        {
            if (room?.GearLevels == null) return 0;
            var total = 0;
            foreach (var level in room.GearLevels) total += Math.Max(1, level) - 1;
            return total;
        }
        public static bool ValidItem(int item) => item >= 0 && item < ItemCount;
        /// <summary>The room size that brings a piece: one more piece for each renovation, spread over the room's sizes.</summary>
        public static int UnlockTier(ClinicState state, int item)
            => 1 + item * (ClinicRules.MaximumTier(state) - 1) / (ItemCount - 1);
        public static bool Unlocked(ClinicState state, ClinicRoom kind, int item)
            => ValidItem(item) && state.Room(kind) != null && state.Room(kind).Tier >= UnlockTier(state, item);
        public static int UnlockedCount(ClinicState state, ClinicRoom kind)
        {
            var count = 0;
            for (var item = 0; item < ItemCount; item++) if (Unlocked(state, kind, item)) count++;
            return count;
        }
        public static bool AtTop(ClinicState state, ClinicRoom kind, int item) => Version(state, kind, item) >= MaximumVersion;

        /// <summary>Price of the next version of a piece; zero when it cannot be upgraded. Compounds on the piece and its version.</summary>
        public static long UpgradeCost(ClinicState state, ClinicRoom kind, int item)
        {
            if (!Active(state, kind) || !ValidItem(item) || AtTop(state, kind, item)) return 0;
            var balance = ClinicBalance.For(state);
            var basis = balance.UpgradeBase[(int)kind, (int)UpgradeTrack.Equipment] * ClinicRules.LocationMultiplier(state);
            return ClinicRules.Compound(ClinicRules.Compound(basis, balance.GearItemGrowth, item), balance.GearVersionGrowth, Version(state, kind, item) - 1);
        }

        /// <summary>Keep the coarse levels in step with the equipment: the equipment level (used by goals and the doctors
        /// clinic unlock) reaches its top with the last upgrade, and facilities follow it, never falling.</summary>
        public static void Sync(ClinicState state, ClinicRoomState room)
        {
            var top = ClinicRules.MaximumTrackLevel(state);
            var derived = 1 + Steps(room) * (top - 1) / TotalSteps;
            room.EquipmentLevel = Math.Max(room.EquipmentLevel, Math.Min(top, derived));
            room.FacilitiesLevel = Math.Max(room.FacilitiesLevel, room.EquipmentLevel);
        }

        public static bool IsValid(ClinicRoomState room)
        {
            var levels = room.GearLevels;
            if (levels == null || levels.Count == 0) return true;
            if (levels.Count != ItemCount) return false;
            foreach (var level in levels) if (level < 1 || level > MaximumVersion) return false;
            return true;
        }

        public static string ItemName(ClinicRoom kind, int item) => ValidItem(item) ? Names[(int)kind][item] : "Equipment";
        public static string VersionName(int version) => version >= 1 && version <= MaximumVersion ? VersionNames[version - 1] : "";
        private static readonly string[][] Names =
        {
            new[] { "Appointment book", "Desk bell", "Ticket dispenser", "Reception computer", "Receipt printer", "Card reader", "Brochure rack", "Wall clock", "Notice board", "Sanitizer stand",
                    "Sign-in tablet", "Security camera", "Water cooler", "Umbrella stand", "Magazine table", "Reception planter", "Wall screen", "Air purifier", "Cash safe", "Queue display" },
            new[] { "First aid kit", "Bandage rack", "Exam lamp", "Blood pressure monitor", "Thermometer station", "Sterilizer", "Dressing trolley", "Wash basin", "Oxygen cylinder", "Defibrillator",
                    "Pulse oximeter", "Stethoscope wall", "Medicine cabinet", "Cold pack fridge", "Splint rack", "Wheelchair", "Stretcher", "IV drip stand", "ECG cart", "Portable scanner" },
            new[] { "Reading rack", "Water dispenser", "Coat rack", "Waiting room TV", "Floor lamp", "Toy corner", "Coffee table", "Magazine stand", "Potted tree", "Fish tank",
                    "Snack shelf", "Wall art", "Charging station", "Standing fan", "Info kiosk", "Kids table", "Bookcase", "Side sofa", "Room divider", "Tea trolley" },
            new[] { "Exam couch", "Doctor's desk", "Desktop computer", "Otoscope set", "Wall blood pressure unit", "Weighing scale", "Height chart", "Skeleton model", "X-ray light box", "Anatomy poster",
                    "Privacy screen", "Consulting sink", "Doctor's stool", "Printer", "Filing cabinet", "Exam light", "ECG monitor", "Ultrasound cart", "Nebulizer", "Laptop cart" },
            new[] { "Dispensing counter", "Pill counting tray", "Shelving", "Medicine fridge", "Label printer", "Precision scale", "Controlled drugs safe", "Barcode scanner", "Mortar and pestle", "Pill robot",
                    "Blister packer", "Syrup shelf", "Counter display", "Vitamin rack", "Advice screen", "Ticket machine", "Bottle washer", "Herb cabinet", "Delivery trolley", "Automated cabinet" }
        };
        private static readonly string[] VersionNames =
        {
            "Basic", "Sturdy", "Standard", "Reinforced", "Professional", "Premium", "Elite", "Master", "Expert", "Advanced"
        };
    }
}
