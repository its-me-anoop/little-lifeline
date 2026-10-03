using System;
using System.Collections.Generic;

namespace IdleClinic.Progression
{
    public sealed class RoomState
    {
        public bool IsClean;
        public int Level;
        public int[] FurnitureLevels;
        public bool StaffHired;
        // Rooms start full of empty open boxes; cleaning clears them along with the dust.
        public bool HasEmptyBoxes => !IsClean;
    }

    /// <summary>Everything the rules read and write. No time, no Unity, no behaviour beyond lookups.</summary>
    public sealed class ProgressionState
    {
        private readonly Dictionary<RoomId, RoomState> rooms = new Dictionary<RoomId, RoomState>();
        private readonly HashSet<RoomId> unlocked = new HashSet<RoomId>();

        public long Wallet;
        public int UpgradesPurchased;
        public int PatientsWaiting;

        public RoomState Room(RoomId id) => rooms[id];
        public bool IsUnlocked(RoomId id) => unlocked.Contains(id);
        public void Unlock(RoomId id) => unlocked.Add(id);

        public static ProgressionState NewGame(ProgressionSettings settings)
        {
            var state = new ProgressionState { Wallet = settings.StartingWallet };
            foreach (var definition in RoomCatalog.All)
            {
                state.rooms[definition.Id] = new RoomState { FurnitureLevels = new int[definition.Furniture.Length] };
            }
            UnlockRules.Apply(state);
            return state;
        }
    }

    public sealed class ProgressionSettings
    {
        public long StartingWallet = 50;
        public long BaseUpgradeCost = 10;
        public long HireCost = 15;
        public double CleanSeconds = 5;
        public double BuildSeconds = 4;
        public double FurnitureSeconds = 2;
        public double HireSeconds = 1;
        public double TravelSeconds = 2;
        // How many patients can wait for a nurse: reception's standing room, plus seats per level of a built waiting room.
        public int BaseWaitingCapacity = 4;
        public int WaitingCapacityPerLevel = 4;
        public double ArrivalSeconds = 4;
        public long CheckInFee = 10;
        public double TreatmentSeconds = 8;
        public long TreatmentFee = 10;
        public double ParkingArrivalBoostPerLevel = 0.25;
        // Each upgrade bought lifts every payment by this fraction of the base fee, so spending pays back.
        public double IncomeBoostPerUpgrade = 0.5;
    }
}
