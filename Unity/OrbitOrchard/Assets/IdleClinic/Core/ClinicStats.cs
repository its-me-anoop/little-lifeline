using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IdleClinic.Core
{
    /// <summary>The numbers a player feels in a clinic: how long each service takes, what a visit pays and how many people fit.
    /// Compared before and after a purchase, so the game can say exactly what changed.</summary>
    public sealed class ClinicStats
    {
        public int CheckInTicks, TreatmentTicks, ConsultationTicks, DispensingTicks, CallTicks;
        public long VisitFee;
        public int QueuePlaces, WaitingSeats, ParkingBays, ClinicLevel;
        public int Receptionists, Nurses, Doctors, Pharmacists;

        public static ClinicStats Of(ClinicState state)
        {
            int Fastest(ClinicStaffRole role) => state.Staff.Where(s => s.Role == role).Select(s => ClinicRules.StationServiceTicks(state, role, s.StationId))
                .DefaultIfEmpty(0).Min();
            int Count(ClinicStaffRole role) => state.Staff.Count(s => s.Role == role);
            return new ClinicStats
            {
                CheckInTicks = Fastest(ClinicStaffRole.Receptionist), TreatmentTicks = Fastest(ClinicStaffRole.Nurse),
                ConsultationTicks = Fastest(ClinicStaffRole.Doctor), DispensingTicks = Fastest(ClinicStaffRole.Pharmacist),
                CallTicks = state.Room(ClinicRoom.Waiting)?.Built == true ? ClinicRules.WaitingCallTicks(state) : 0,
                VisitFee = ClinicRules.VisitFee(state),
                QueuePlaces = ClinicRules.UnpaidQueueCapacity(state), WaitingSeats = state.Room(ClinicRoom.Waiting)?.Built == true ? ClinicRules.WaitingCapacity(state) : 0,
                ParkingBays = state.Amenity(ClinicAmenity.Parking) == null ? 0 : ClinicRules.ParkingCapacity(state),
                ClinicLevel = state.OfficeLevel,
                Receptionists = Count(ClinicStaffRole.Receptionist), Nurses = Count(ClinicStaffRole.Nurse),
                Doctors = Count(ClinicStaffRole.Doctor), Pharmacists = Count(ClinicStaffRole.Pharmacist)
            };
        }

        /// <summary>The room's service time at its fastest staffed station (station 0 before anyone is hired), now or after
        /// <paramref name="gearSteps"/> more equipment upgrades (rules 5). The panel shows both, so the arrow compares like with like.</summary>
        public static int ServiceTicks(ClinicState state, ClinicRoom room, int gearSteps = 0)
        {
            if (room == ClinicRoom.Waiting)
                return ClinicGear.Active(state, room) ? ClinicGear.Apply(state, room, ClinicGear.BaseTicks(state, room), gearSteps) : ClinicRules.WaitingCallTicks(state);
            var role = room == ClinicRoom.Reception ? ClinicStaffRole.Receptionist : room == ClinicRoom.FirstAid ? ClinicStaffRole.Nurse
                : room == ClinicRoom.Consultation ? ClinicStaffRole.Doctor : ClinicStaffRole.Pharmacist;
            var stations = state.Staff.Where(s => s.Role == role).Select(s => s.StationId).DefaultIfEmpty(0);
            return stations.Select(id => ClinicRules.StationServiceTicks(state, role, id, gearStepsAdded: gearSteps)).Min();
        }

        /// <summary>The exact visit fee after one more decor level in <paramref name="room"/>, computed with the game's own
        /// rounding. The state is restored before returning.</summary>
        public static long VisitFeeAfterDecor(ClinicState state, ClinicRoom room)
        {
            var target = state.Room(room);
            if (target == null) return ClinicRules.VisitFee(state);
            target.DecorationLevel++;
            try { return ClinicRules.VisitFee(state); }
            finally { target.DecorationLevel--; }
        }

        /// <summary>The service time after one more equipment upgrade in this room.</summary>
        public static int ServiceTicksAfterGear(ClinicState state, ClinicRoom room) => ServiceTicks(state, room, 1);

        /// <summary>What changed, most noticeable first, in words a player reads at a glance.</summary>
        public static IReadOnlyList<string> Changes(ClinicStats before, ClinicStats after)
        {
            var changes = new List<string>();
            void Time(string name, int a, int b) { if (a > 0 && b > 0 && a != b) changes.Add(name + " " + Seconds(a) + " → " + Seconds(b)); }
            void Number(string name, long a, long b, string unit = "") { if (a != b) changes.Add(name + " " + a.ToString("N0", CultureInfo.InvariantCulture) + " → " + b.ToString("N0", CultureInfo.InvariantCulture) + unit); }
            Number("Clinic level", before.ClinicLevel, after.ClinicLevel);
            Number("Receptionists", before.Receptionists, after.Receptionists);
            Number("Nurses", before.Nurses, after.Nurses);
            Number("Doctors", before.Doctors, after.Doctors);
            Number("Pharmacists", before.Pharmacists, after.Pharmacists);
            Time("Check-in", before.CheckInTicks, after.CheckInTicks);
            Time("Treatment", before.TreatmentTicks, after.TreatmentTicks);
            Time("Consultation", before.ConsultationTicks, after.ConsultationTicks);
            Time("Dispensing", before.DispensingTicks, after.DispensingTicks);
            Time("Call from a seat", before.CallTicks, after.CallTicks);
            Number("Visit fee", before.VisitFee, after.VisitFee, " coins");
            Number("Queue places", before.QueuePlaces, after.QueuePlaces);
            Number("Waiting seats", before.WaitingSeats, after.WaitingSeats);
            Number("Parking bays", before.ParkingBays, after.ParkingBays);
            return changes;
        }

        private static string Seconds(int ticks) => (ticks / (double)ClinicRules.TicksPerSecond).ToString("0.0", CultureInfo.InvariantCulture) + "s";
    }
}
