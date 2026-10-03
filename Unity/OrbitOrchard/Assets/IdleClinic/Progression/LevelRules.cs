using System.Linq;

namespace IdleClinic.Progression
{
    /// <summary>Which rooms may move up a level, and what "fully upgraded for a level" means.</summary>
    public static class LevelRules
    {
        /// <summary>Built to the level, every piece of furniture at the level, and its staff member hired.</summary>
        public static bool IsComplete(ProgressionState state, RoomId id, int level)
        {
            var room = state.Room(id);
            if (room.Level < level) return false;
            if (room.FurnitureLevels.Any(f => f < level)) return false;
            return level < 1 || RoomCatalog.Definition(id).Staff == StaffRole.None || room.StaffHired;
        }

        /// <summary>The office leads. It moves up only when every open room is complete; the others follow it.</summary>
        public static bool CanLevelUp(ProgressionState state, RoomId id)
        {
            if (!state.IsUnlocked(id)) return false;
            var room = state.Room(id);
            if (!room.IsClean) return false;
            if (id != RoomId.Office) return state.Room(RoomId.Office).Level > room.Level;
            return RoomCatalog.All.Where(d => state.IsUnlocked(d.Id)).All(d => IsComplete(state, d.Id, room.Level));
        }

        public static bool CanUpgradeFurniture(ProgressionState state, RoomId id, int furnitureIndex)
        {
            var room = state.Room(id);
            return state.IsUnlocked(id) && room.IsClean && room.FurnitureLevels[furnitureIndex] < room.Level;
        }

        public static bool NeedsHire(ProgressionState state, RoomId id)
        {
            var room = state.Room(id);
            return state.IsUnlocked(id) && room.Level >= 1 && !room.StaffHired
                && RoomCatalog.Definition(id).Staff != StaffRole.None;
        }
    }
}
