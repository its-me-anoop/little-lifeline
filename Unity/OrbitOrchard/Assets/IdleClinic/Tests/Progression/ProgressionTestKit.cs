namespace IdleClinic.Progression.Tests
{
    /// <summary>Shortcuts for putting a new game into a known mid-game position.</summary>
    internal static class ProgressionTestKit
    {
        public static ProgressionState NewGame() => ProgressionState.NewGame(new ProgressionSettings());

        public static void Complete(ProgressionState state, RoomId id, int level)
        {
            var room = state.Room(id);
            room.IsClean = true;
            room.Level = level;
            for (var i = 0; i < room.FurnitureLevels.Length; i++) room.FurnitureLevels[i] = level;
            if (level >= 1 && RoomCatalog.Definition(id).Staff != StaffRole.None) room.StaffHired = true;
        }

        public static void BuildStarterRooms(ProgressionState state, int level)
        {
            Complete(state, RoomId.Office, level);
            Complete(state, RoomId.Reception, level);
            Complete(state, RoomId.NursingStation1, level);
        }
    }
}
