using System;

namespace IdleClinic.Progression
{
    /// <summary>Everything needed to carry on a game from where it stopped. Plain public fields so any serializer can store it.</summary>
    [Serializable]
    public sealed class ProgressionSnapshot
    {
        public int Version = 1;
        public long Wallet;
        public int UpgradesPurchased;
        public int PatientsWaiting;
        public RoomSnapshot[] Rooms = new RoomSnapshot[0];
        public BossSnapshot Boss = new BossSnapshot();
        public FlowSnapshot Patients = new FlowSnapshot();
    }

    [Serializable]
    public sealed class RoomSnapshot
    {
        public int Id;
        public bool Unlocked;
        public bool IsClean;
        public int Level;
        public int[] Furniture = new int[0];
        public bool StaffHired;
    }

    [Serializable]
    public sealed class BossSnapshot
    {
        public int Location;
        public bool HasTask;
        public int TaskKind, TaskRoom, TaskFurniture;
        public bool Travelling;
        public int Destination;
        public double Remaining;
    }

    [Serializable]
    public sealed class FlowSnapshot
    {
        public bool Armed;
        public double UntilArrival;
        public bool[] Treating = new bool[2];
        public double[] TreatmentRemaining = new double[2];
    }
}
