using System;

namespace IdleClinic.Progression
{
    /// <summary>Composition root for the progression rules: owns the state, the boss and the patients, and advances them together.</summary>
    public sealed class ClinicProgression
    {
        private const double MaxStep = 0.1;
        private readonly BossWorker boss;
        private readonly PatientFlow patients;

        public event Action<ProgressionEvent> Occurred;

        public ProgressionState State { get; }
        public BossTask? BossTask => boss.Current;
        public double BossProgress => boss.Progress;

        public ClinicProgression(ProgressionSettings settings)
        {
            State = ProgressionState.NewGame(settings);
            boss = new BossWorker(State, new BossPlanner(), new DoublingCostPolicy(settings.BaseUpgradeCost), settings);
            patients = new PatientFlow(State, settings);
            boss.Occurred += Raise;
            patients.Occurred += Raise;
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
