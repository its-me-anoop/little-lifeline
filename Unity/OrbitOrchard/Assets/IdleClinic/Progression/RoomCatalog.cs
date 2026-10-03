using System;
using System.Collections.Generic;

namespace IdleClinic.Progression
{
    public enum RoomId { Office, Reception, NursingStation1, Waiting, NursingStation2, Parking }

    public enum StaffRole { None, Receptionist, Nurse }

    /// <summary>What a room is made of and when it opens. Order matters: it is the order the boss works in.</summary>
    public sealed class RoomDefinition
    {
        public RoomId Id;
        public string Name;
        public string[] Furniture;
        public StaffRole Staff;
        public IUnlockRule Unlock;
    }

    public static class RoomCatalog
    {
        private static readonly RoomDefinition[] all =
        {
            new RoomDefinition { Id = RoomId.Office, Name = "Office", Furniture = new[] { "Desk", "Bookshelf" }, Unlock = UnlockRule.Always },
            new RoomDefinition { Id = RoomId.Reception, Name = "Reception", Furniture = new[] { "Counter", "Computer" }, Staff = StaffRole.Receptionist, Unlock = UnlockRule.Always },
            new RoomDefinition { Id = RoomId.NursingStation1, Name = "Nursing station 1", Furniture = new[] { "Bed", "Cabinet" }, Staff = StaffRole.Nurse, Unlock = UnlockRule.Always },
            new RoomDefinition { Id = RoomId.Waiting, Name = "Waiting room", Furniture = new[] { "Seats", "Plants" }, Unlock = UnlockRule.PatientsWaitingAbove(2) },
            new RoomDefinition { Id = RoomId.NursingStation2, Name = "Nursing station 2", Furniture = new[] { "Bed", "Cabinet" }, Staff = StaffRole.Nurse, Unlock = UnlockRule.OfficeAtLevel(2) },
            new RoomDefinition { Id = RoomId.Parking, Name = "Parking", Furniture = new[] { "Barrier", "Lamps" }, Unlock = UnlockRule.OfficeAtLevel(2) },
        };

        public static IReadOnlyList<RoomDefinition> All => all;

        public static RoomDefinition Definition(RoomId id) => all[(int)id];
    }
}
