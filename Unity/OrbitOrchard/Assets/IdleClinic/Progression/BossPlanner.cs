namespace IdleClinic.Progression
{
    public enum BossTaskKind { Clean, LevelUp, UpgradeFurniture, Hire }

    public struct BossTask
    {
        public BossTaskKind Kind;
        public RoomId Room;
        public int FurnitureIndex;
    }

    public interface IBossPlanner
    {
        BossTask? Next(ProgressionState state);
    }

    /// <summary>
    /// Rooms are visited in catalog order, clean, then build, then staff. Furniture comes after every room
    /// has had its turn, so a fresh room is never held up by furniture elsewhere.
    /// </summary>
    public sealed class BossPlanner : IBossPlanner
    {
        public BossTask? Next(ProgressionState state)
        {
            foreach (var definition in RoomCatalog.All)
            {
                var id = definition.Id;
                if (!state.IsUnlocked(id)) continue;
                if (!state.Room(id).IsClean) return new BossTask { Kind = BossTaskKind.Clean, Room = id };
                if (LevelRules.CanLevelUp(state, id)) return new BossTask { Kind = BossTaskKind.LevelUp, Room = id };
                if (LevelRules.NeedsHire(state, id)) return new BossTask { Kind = BossTaskKind.Hire, Room = id };
            }
            foreach (var definition in RoomCatalog.All)
            {
                var furniture = state.Room(definition.Id).FurnitureLevels;
                for (var i = 0; i < furniture.Length; i++)
                    if (LevelRules.CanUpgradeFurniture(state, definition.Id, i))
                        return new BossTask { Kind = BossTaskKind.UpgradeFurniture, Room = definition.Id, FurnitureIndex = i };
            }
            return null;
        }
    }
}
