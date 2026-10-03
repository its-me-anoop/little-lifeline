using System;

namespace IdleClinic.Progression
{
    /// <summary>Composition root for the progression rules: owns the state, the boss and the patients, and advances them together.</summary>
    public sealed class ClinicProgression
    {
        private const double MaxStep = 0.1;
        private readonly BossWorker boss;
        private readonly PatientFlow patients;
        private readonly IBossPlanner planner = new BossPlanner();
        private readonly ICostPolicy costs;

        public event Action<ProgressionEvent> Occurred;

        public ProgressionState State { get; }
        public BossTask? BossTask => boss.Current;
        public double BossProgress => boss.Progress;
        public BossTask? NextTask => planner.Next(State);
        public long CostOf(BossTask task) => boss.CostOf(task);
        public int WaitingCapacity => patients.Capacity;
        public long NextUpgradeCost => costs.CostOfUpgrade(State.UpgradesPurchased);
        public bool BossTravelling => boss.IsTravelling;
        public double BossTravelProgress => boss.TravelProgress;
        public RoomId BossLocation => boss.Location;
        public RoomId? BossDestination => boss.Destination;

        public ClinicProgression(ProgressionSettings settings)
        {
            State = ProgressionState.NewGame(settings);
            costs = new DoublingCostPolicy(settings.BaseUpgradeCost);
            boss = new BossWorker(State, planner, costs, settings);
            patients = new PatientFlow(State, settings);
            boss.Occurred += Raise;
            patients.Occurred += Raise;
        }

        public ProgressionSnapshot Capture()
        {
            var snapshot = new ProgressionSnapshot();
            State.CaptureInto(snapshot); boss.CaptureInto(snapshot.Boss); patients.CaptureInto(snapshot.Patients);
            return snapshot;
        }

        /// <summary>Carries on from a saved game. A missing snapshot leaves the game as it is.</summary>
        public void Restore(ProgressionSnapshot snapshot)
        {
            if (snapshot == null || snapshot.Rooms == null) return;
            State.RestoreFrom(snapshot); boss.RestoreFrom(snapshot.Boss); patients.RestoreFrom(snapshot.Patients);
        }

        public void Tick(double seconds)
        {
            // Fixed steps keep the result identical however the frames fall.
            while (seconds > 1e-9)
            {
                var step = Math.Min(seconds, MaxStep);
                patients.Tick(step);
                boss.Tick(step);
                seconds -= step;
            }
        }

        private void Raise(ProgressionEvent e) => Occurred?.Invoke(e);
    }
}
