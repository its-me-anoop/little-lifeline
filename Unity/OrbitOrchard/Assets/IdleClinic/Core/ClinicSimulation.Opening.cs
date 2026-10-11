using System;
using System.Linq;

namespace IdleClinic.Core
{
    public enum ClinicOpeningStep
    {
        CleanOffice, SetUpOffice, CleanReception, SetUpReception, FirstCheckIn,
        CleanWaiting, SetUpWaiting, CleanNursing, SetUpNursing, HireNurse,
        QueueArrivals, HireReceptionist, Complete
    }

    public sealed partial class ClinicSimulation
    {
        public static ClinicSimulation CreateOpening(ulong seed = 42)
        {
            var state = new ClinicState { Seed = seed, NextPatientId = 1, OpeningEnabled = true,
                Tutorial = ClinicTutorialStep.BossOpening };
            foreach (var kind in new[] { ClinicRoom.Reception, ClinicRoom.FirstAid, ClinicRoom.Waiting })
                state.Rooms.Add(new ClinicRoomState { Kind = kind });
            foreach (var kind in new[] { ClinicAmenity.Parking, ClinicAmenity.Toilet, ClinicAmenity.Vending })
                state.Amenities.Add(new ClinicAmenityState { Kind = kind });
            return new ClinicSimulation(state);
        }

        public ClinicCommandResult ContinueOpening(ClinicOpeningStep expected)
        {
            if (!State.OpeningEnabled || State.OpeningStep != expected || State.OpeningTaskEndsTick >= 0)
                return No("Follow the current opening task.");
            if (expected == ClinicOpeningStep.HireNurse) return HireNurse();
            if (expected == ClinicOpeningStep.HireReceptionist) return HireReceptionist();
            if (!OpeningManual(expected)) return No("Your clinic is getting ready.");
            State.BossFromZone = State.BossZone;
            State.BossZone = OpeningZone(expected);
            State.OpeningTaskStartedTick = State.Tick;
            State.OpeningTaskEndsTick = State.Tick + OpeningTaskTicks(expected);
            Emit(ClinicEventKind.TutorialAdvanced, OpeningRoom(expected));
            return Yes(OpeningCleaning(expected) ? "The boss is cleaning." : "The boss is setting up the room.");
        }

        /// <summary>Ticks of boss work for a task. The office's clean and set-up show the rhythm; later rooms repeat it more briskly
        /// (4.3 build 22 used 8s and 10s for every room). Saves started under the longer timers finish them unchanged.</summary>
        public static int OpeningTaskTicks(ClinicOpeningStep step)
            => step == ClinicOpeningStep.CleanOffice ? 60 : step == ClinicOpeningStep.SetUpOffice ? 80 : OpeningCleaning(step) ? 40 : 50;
        /// <summary>The longest a task can have been given by any version, for saves made mid-task.</summary>
        private static int LongestOpeningTaskTicks(ClinicOpeningStep step) => OpeningCleaning(step) ? 80 : 100;
        /// <summary>Each tap on Help takes this much off the boss's current task.</summary>
        public const int OpeningHelpTicks = 15;

        /// <summary>The player lends a hand: the boss's current task finishes a little sooner. Optional; waiting works too.</summary>
        public ClinicCommandResult HelpOpeningTask()
        {
            if (!State.OpeningEnabled || State.OpeningStep == ClinicOpeningStep.Complete || State.OpeningTaskEndsTick < 0)
                return No("The boss is not working on anything right now.");
            if (State.OpeningTaskEndsTick - State.Tick <= 1) return No("Nearly done.");
            State.OpeningTaskEndsTick = Math.Max(State.Tick + 1, State.OpeningTaskEndsTick - OpeningHelpTicks);
            return Yes(OpeningCleaning(State.OpeningStep) ? "You help clear up." : "You help set up.");
        }

        private void FinishOpeningTask()
        {
            if (!State.OpeningEnabled || State.OpeningTaskEndsTick < 0 || State.OpeningTaskEndsTick > State.Tick) return;
            var step = State.OpeningStep;
            State.OpeningTaskEndsTick = -1;
            if (step == ClinicOpeningStep.CleanOffice) State.OfficeCleaned = true;
            else if (step == ClinicOpeningStep.SetUpOffice) State.OfficeLevel = 1;
            else
            {
                var room = State.Room(OpeningRoom(step));
                if (OpeningCleaning(step)) room.Cleaned = true;
                else
                {
                    room.Built = true;
                    if (room.Kind == ClinicRoom.Waiting) State.WaitingRoomUnlocked = true;
                    else room.StationCount = 1;
                    if (room.Kind == ClinicRoom.Reception)
                    {
                        State.ReceptionDesks.Add(new ReceptionDeskState { Id = 0 });
                        State.Patients.Add(new ClinicPatientState { Id = 0, AppearanceId = ClinicRules.PatientAppearance(State.Seed,0),
                            Phase = ClinicPatientPhase.Arriving, QueueIndex = 0, FromAnchor = "entrance",
                            ToAnchor = ClinicRules.QueueAnchor(0), ArrivalTick = State.Tick,
                            PhaseStartedTick = State.Tick, PhaseEndsTick = State.Tick + 30 });
                    }
                    if (room.Kind == ClinicRoom.FirstAid) State.TreatmentStations.Add(new TreatmentStationState { Id = 0 });
                }
            }
            State.OpeningStep++;
            Emit(ClinicEventKind.TutorialAdvanced, OpeningRoom(State.OpeningStep));
        }

        private ClinicCommandResult HireOpeningStaff(ClinicStaffRole role)
        {
            var required = role == ClinicStaffRole.Nurse ? ClinicOpeningStep.HireNurse : ClinicOpeningStep.HireReceptionist;
            if (State.OpeningStep != required || State.OpeningTaskEndsTick >= 0) return No("Finish the current opening task first.");
            var staff = NewStaff(ClinicRules.StaffId(role,0),role,0);
            State.Staff.Add(staff);
            // The first team is included in the opening kit. Subsequent hires use the normal prices.
            Emit(role == ClinicStaffRole.Nurse ? ClinicEventKind.NurseHired : ClinicEventKind.ReceptionistHired,
                ClinicRules.RoomForRole(role),staffId:staff.Id,source:"entrance");
            State.OpeningStep++;
            State.BossFromZone = State.BossZone;State.BossZone = 3;
            if (role == ClinicStaffRole.Nurse) State.NextArrivalTick = State.Tick + 10;
            else Tutorial(ClinicTutorialStep.Complete);
            return Yes(role == ClinicStaffRole.Nurse ? "Your first nurse is hired." : "Your first receptionist is hired.");
        }

        private void AdvanceOpeningQueue()
        {
            if (State.OpeningEnabled && State.OpeningStep == ClinicOpeningStep.QueueArrivals && State.Patients.Count(IsUnpaidQueue) >= 2)
            { State.OpeningStep = ClinicOpeningStep.HireReceptionist;Emit(ClinicEventKind.TutorialAdvanced,ClinicRoom.Reception); }
        }

        public static bool OpeningCleaning(ClinicOpeningStep step) => step == ClinicOpeningStep.CleanOffice || step == ClinicOpeningStep.CleanReception
            || step == ClinicOpeningStep.CleanWaiting || step == ClinicOpeningStep.CleanNursing;
        public static bool OpeningManual(ClinicOpeningStep step) => OpeningCleaning(step) || step == ClinicOpeningStep.SetUpOffice
            || step == ClinicOpeningStep.SetUpReception || step == ClinicOpeningStep.SetUpWaiting || step == ClinicOpeningStep.SetUpNursing;
        public static int OpeningZone(ClinicOpeningStep step) => step <= ClinicOpeningStep.SetUpOffice || step == ClinicOpeningStep.Complete ? 3
            : step <= ClinicOpeningStep.FirstCheckIn || step >= ClinicOpeningStep.QueueArrivals ? 0
            : step <= ClinicOpeningStep.SetUpWaiting ? 2 : 1;
        public static ClinicRoom OpeningRoom(ClinicOpeningStep step) => (ClinicRoom)Math.Min(2,OpeningZone(step));

        private static bool ValidOpening(ClinicState state)
        {
            if (!Enum.IsDefined(typeof(ClinicOpeningStep),state.OpeningStep) || state.OfficeLevel < 0 || state.OfficeLevel > ClinicRules.MaximumTier(state)
                || state.BossZone < 0 || state.BossZone > 3 || state.BossFromZone < 0 || state.BossFromZone > 3
                || state.OpeningTaskStartedTick < 0 || state.OpeningTaskStartedTick > state.Tick
                || state.OpeningTaskEndsTick != -1 && (state.OpeningTaskEndsTick <= state.Tick || !OpeningManual(state.OpeningStep)
                    || state.OpeningTaskEndsTick-state.OpeningTaskStartedTick < 1
                    || state.OpeningTaskEndsTick-state.OpeningTaskStartedTick > LongestOpeningTaskTicks(state.OpeningStep))) return false;
            var step=state.OpeningStep;
            if (state.OfficeCleaned != (step > ClinicOpeningStep.CleanOffice) || (step == ClinicOpeningStep.Complete ? state.OfficeLevel < 1 : state.OfficeLevel != (step > ClinicOpeningStep.SetUpOffice ? 1 : 0))) return false;
            if (step == ClinicOpeningStep.Complete) return state.Tutorial == ClinicTutorialStep.Complete;
            if (state.Tutorial != ClinicTutorialStep.BossOpening || state.Construction.Count != 0 || state.TotalPayments > 1) return false;
            var reception=state.Room(ClinicRoom.Reception);var waiting=state.Room(ClinicRoom.Waiting);var nursing=state.Room(ClinicRoom.FirstAid);
            if (reception.Cleaned != (step > ClinicOpeningStep.CleanReception) || reception.Built != (step > ClinicOpeningStep.SetUpReception)
                || waiting.Cleaned != (step > ClinicOpeningStep.CleanWaiting) || waiting.Built != (step > ClinicOpeningStep.SetUpWaiting)
                || nursing.Cleaned != (step > ClinicOpeningStep.CleanNursing) || nursing.Built != (step > ClinicOpeningStep.SetUpNursing)
                || state.TreatmentStations.Count != (nursing.Built?1:0) || state.ReceptionDesks.Count != (reception.Built?1:0)
                || state.Staff.Count(s=>s.Role==ClinicStaffRole.Receptionist) != 0
                || state.Staff.Count(s=>s.Role==ClinicStaffRole.Nurse) != (step > ClinicOpeningStep.HireNurse?1:0)
                || (step <= ClinicOpeningStep.HireNurse && state.NextArrivalTick != -1)
                || (step > ClinicOpeningStep.HireNurse && state.NextArrivalTick <= state.Tick)
                || (step <= ClinicOpeningStep.SetUpReception && state.Patients.Count != 0)
                || (step < ClinicOpeningStep.CleanWaiting && state.TotalPayments != 0)
                || (step >= ClinicOpeningStep.CleanWaiting && state.TotalPayments != 1)) return false;
            return true;
        }
    }
}
