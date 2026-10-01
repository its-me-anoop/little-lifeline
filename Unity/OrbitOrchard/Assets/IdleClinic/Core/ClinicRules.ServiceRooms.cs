using System;

namespace IdleClinic.Core
{
    /// <summary>The starter clinic's back-of-house rooms (rules 5): an office, a staff room and a store. Each is built once
    /// for coins, then renovated and equipped like the other rooms, twenty pieces of ten versions each. Every equipment
    /// upgrade in the office raises the visit fee, in the staff room makes every member of staff a little faster, and in
    /// the store adds, step by step, queue places and corridor seats.</summary>
    public static partial class ClinicRules
    {
        public static readonly ClinicRoom[] ServiceRooms = { ClinicRoom.Office, ClinicRoom.StaffRoom, ClinicRoom.Store };
        public static bool IsServiceRoom(ClinicRoom kind) => kind == ClinicRoom.Office || kind == ClinicRoom.StaffRoom || kind == ClinicRoom.Store;
        /// <summary>Whether this clinic has the office, staff room and store to build (the starter clinic under rules 5).</summary>
        public static bool OffersServiceRooms(ClinicState state) => state != null && Deep(state) && !IsDoctors(state);
        /// <summary>The room that must be built first: the lounge, then the office, then the staff room.</summary>
        public static ClinicRoom ServiceRoomPrerequisite(ClinicRoom kind) => kind == ClinicRoom.Office ? ClinicRoom.Waiting
            : kind == ClinicRoom.StaffRoom ? ClinicRoom.Office : ClinicRoom.StaffRoom;
        public static string RoomLabel(ClinicRoom kind) => kind == ClinicRoom.FirstAid ? "first aid room" : kind == ClinicRoom.Waiting ? "waiting room"
            : kind == ClinicRoom.StaffRoom ? "staff room" : kind.ToString().ToLowerInvariant();

        public static long RoomBuildCost(ClinicState state, ClinicRoom kind)
            => IsServiceRoom(kind) ? ClinicBalance.For(state).RoomBuildCost[(int)kind] * LocationMultiplier(state) : 0;
        public static int RoomBuildSeconds(ClinicState state, ClinicRoom kind)
            => IsServiceRoom(kind) ? ClinicBalance.For(state).RoomBuildSeconds[(int)kind] : 0;

        /// <summary>Equipment upgrades owned in a built service room (zero otherwise).</summary>
        public static int ServiceSteps(ClinicState state, ClinicRoom kind, int stepsAdded = 0)
        {
            if (!OffersServiceRooms(state)) return 0;
            var room = state.Room(kind);
            if (room == null || !room.Built) return 0;
            return Math.Max(0, Math.Min(ClinicGear.TotalSteps, ClinicGear.Steps(state, kind) + stepsAdded));
        }
        private static int Share(int maximum, int steps) => (int)((long)maximum * steps / ClinicGear.TotalSteps);

        /// <summary>Percentage the office adds to the visit fee.</summary>
        public static int OfficeFeePercent(ClinicState state, int stepsAdded = 0)
            => Share(ClinicBalance.For(state).OfficeFeePercent, ServiceSteps(state, ClinicRoom.Office, stepsAdded));
        /// <summary>Percentage the staff room takes off every service time.</summary>
        public static int StaffRoomSpeedPercent(ClinicState state, int stepsAdded = 0)
            => Share(ClinicBalance.For(state).StaffRoomSpeedPercent, ServiceSteps(state, ClinicRoom.StaffRoom, stepsAdded));
        /// <summary>Queue places the store adds to the reception.</summary>
        public static int StoreQueuePlaces(ClinicState state, int stepsAdded = 0)
            => Share(ClinicBalance.For(state).StoreQueuePlaces, ServiceSteps(state, ClinicRoom.Store, stepsAdded));
        /// <summary>Corridor seats the store adds to the waiting room.</summary>
        public static int StoreSeats(ClinicState state, int stepsAdded = 0)
            => Share(ClinicBalance.For(state).StoreSeats, ServiceSteps(state, ClinicRoom.Store, stepsAdded));
        /// <summary>A service time after the staff room's rest and refreshment.</summary>
        internal static int Rested(ClinicState state, int ticks)
        {
            var percent = StaffRoomSpeedPercent(state);
            return percent <= 0 ? ticks : (int)(((long)ticks * (100 - percent) + 99) / 100);
        }
    }
}
