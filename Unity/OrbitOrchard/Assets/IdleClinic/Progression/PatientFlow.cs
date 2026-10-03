using System;

namespace IdleClinic.Progression
{
    /// <summary>Patients arrive once reception is staffed, pay at check-in, wait, then pay again when a nurse treats them.</summary>
    public sealed class PatientFlow
    {
        private readonly ProgressionState state;
        private readonly ProgressionSettings settings;
        private readonly double[] treatmentRemaining = new double[2];
        private readonly bool[] treating = new bool[2];
        private static readonly RoomId[] Stations = { RoomId.NursingStation1, RoomId.NursingStation2 };
        private double untilArrival;
        private bool armed;

        public event Action<ProgressionEvent> Occurred;

        public PatientFlow(ProgressionState state, ProgressionSettings settings)
        {
            this.state = state; this.settings = settings;
        }

        public void CaptureInto(FlowSnapshot snapshot)
        {
            snapshot.Armed = armed; snapshot.UntilArrival = untilArrival;
            snapshot.Treating = (bool[])treating.Clone(); snapshot.TreatmentRemaining = (double[])treatmentRemaining.Clone();
        }

        public void RestoreFrom(FlowSnapshot snapshot)
        {
            armed = snapshot.Armed; untilArrival = snapshot.UntilArrival;
            for (var i = 0; i < treating.Length && i < snapshot.Treating.Length; i++)
            {
                treating[i] = snapshot.Treating[i]; treatmentRemaining[i] = snapshot.TreatmentRemaining[i];
            }
        }

        public void Tick(double seconds)
        {
            var receptionOpen = state.Room(RoomId.Reception).StaffHired;
            if (!receptionOpen) { armed = false; }
            else if (!armed) { armed = true; untilArrival = ArrivalInterval(); }

            while (seconds > 1e-9)
            {
                var step = seconds;
                if (armed) step = Math.Min(step, untilArrival);
                for (var i = 0; i < Stations.Length; i++) if (treating[i]) step = Math.Min(step, treatmentRemaining[i]);

                for (var i = 0; i < Stations.Length; i++)
                    if (treating[i]) treatmentRemaining[i] -= step;
                if (armed) untilArrival -= step;
                seconds -= step;

                for (var i = 0; i < Stations.Length; i++)
                    if (treating[i] && treatmentRemaining[i] <= 1e-9) FinishTreatment(i);
                if (armed && untilArrival <= 1e-9) { Arrive(); untilArrival = ArrivalInterval(); }
                StartTreatments();
            }
        }

        public int Capacity => settings.BaseWaitingCapacity
            + (state.IsUnlocked(RoomId.Waiting) ? settings.WaitingCapacityPerLevel * state.Room(RoomId.Waiting).Level : 0);

        private double ArrivalInterval()
        {
            var parking = state.Room(RoomId.Parking).Level;
            return settings.ArrivalSeconds / (1 + settings.ParkingArrivalBoostPerLevel * parking);
        }

        private void Arrive()
        {
            if (state.PatientsWaiting >= Capacity)
            {
                Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.PatientTurnedAway });
                return;
            }
            var fee = Boosted(settings.CheckInFee);
            state.PatientsWaiting++;
            state.Wallet += fee;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.PatientArrived, Payment = fee });
            foreach (var opened in UnlockRules.Apply(state))
                Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.RoomUnlocked, Room = opened });
        }

        private void StartTreatments()
        {
            for (var i = 0; i < Stations.Length && state.PatientsWaiting > 0; i++)
            {
                var room = state.Room(Stations[i]);
                if (treating[i] || room.Level < 1 || !room.StaffHired) continue;
                state.PatientsWaiting--;
                treating[i] = true; treatmentRemaining[i] = settings.TreatmentSeconds;
                Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.TreatmentStarted, Room = Stations[i] });
            }
        }

        private void FinishTreatment(int station)
        {
            treating[station] = false;
            var fee = Boosted(settings.TreatmentFee);
            state.Wallet += fee;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.TreatmentCompleted, Room = Stations[station], Payment = fee });
        }

        private long Boosted(long fee) =>
            (long)Math.Round(fee * (1 + settings.IncomeBoostPerUpgrade * state.UpgradesPurchased));
    }
}
