namespace IdleClinic.Progression
{
    public enum ProgressionEventKind { BossTravelStarted, TaskStarted, TaskCompleted, RoomUnlocked, PatientArrived, PatientTurnedAway, TreatmentStarted, TreatmentCompleted }

    public struct ProgressionEvent
    {
        public ProgressionEventKind Kind;
        public BossTask Task;
        public RoomId Room;
        public long Cost;
        public long Payment;
    }
}
