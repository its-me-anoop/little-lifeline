using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleClinic.Core
{
    public sealed class ClinicMilestone
    {
        public string Id { get; }
        public string Title { get; }
        public long GemReward { get; }
        private readonly Func<ClinicState, ClinicState, bool> met;

        internal ClinicMilestone(string id, string title, long gemReward, Func<ClinicState, ClinicState, bool> met)
        { Id = id; Title = title; GemReward = gemReward; this.met = met; }

        /// <summary>Evaluated from stored progress only, so a player updating from 3.x can claim what they already achieved.</summary>
        public bool IsMet(ClinicState starter, ClinicState doctors) => starter != null && met(starter, doctors);
    }

    /// <summary>Permanent achievements that pay free gems once each. Ids are stored in saves: never rename or reuse one.</summary>
    public static class ClinicMilestones
    {
        public static IReadOnlyList<ClinicMilestone> All { get; } = Build();

        public static ClinicMilestone Find(string id) => All.FirstOrDefault(m => m.Id == id);

        private static List<ClinicMilestone> Build()
        {
            var list = new List<ClinicMilestone>
            {
                new ClinicMilestone("care.first", "Treat your first patient", 5, (s, d) => Treatments(s, d) >= 1),
                new ClinicMilestone("care.100", "Treat 100 patients", 10, (s, d) => Treatments(s, d) >= 100),
                new ClinicMilestone("care.1000", "Treat 1,000 patients", 20, (s, d) => Treatments(s, d) >= 1000),
                new ClinicMilestone("care.10000", "Treat 10,000 patients", 40, (s, d) => Treatments(s, d) >= 10000),
                new ClinicMilestone("earned.10k", "Earn 10,000 coins", 10, (s, d) => Earned(s, d) >= 10000),
                new ClinicMilestone("earned.100k", "Earn 100,000 coins", 25, (s, d) => Earned(s, d) >= 100000),
                new ClinicMilestone("earned.1m", "Earn 1,000,000 coins", 50, (s, d) => Earned(s, d) >= 1000000),
                new ClinicMilestone("waiting.built", "Open the waiting room", 10, (s, d) => s.Room(ClinicRoom.Waiting)?.Built == true),
                new ClinicMilestone("staff.nurses.2", "Hire a second nurse", 10, (s, d) => Count(s, ClinicStaffRole.Nurse) >= 2),
                new ClinicMilestone("staff.reception.2", "Hire a second receptionist", 10, (s, d) => Count(s, ClinicStaffRole.Receptionist) >= 2),
                new ClinicMilestone("starter.complete", "Complete the starter clinic", 30, (s, d) => d != null || ClinicRules.StarterCompletion(s).Count == 0),
                new ClinicMilestone("doctors.open", "Open the doctors clinic", 50, (s, d) => d != null),
                new ClinicMilestone("care.25000", "Treat 25,000 patients", 60, (s, d) => Treatments(s, d) >= 25000),
                new ClinicMilestone("care.50000", "Treat 50,000 patients", 80, (s, d) => Treatments(s, d) >= 50000),
                new ClinicMilestone("earned.10m", "Earn 10,000,000 coins", 80, (s, d) => Earned(s, d) >= 10000000),
                new ClinicMilestone("doctors.parking.max", "Fully upgrade the doctors car park", 25,
                    (s, d) => d != null && d.Amenity(ClinicAmenity.Parking)?.Level >= ClinicRules.MaximumAmenityLevel(d, ClinicAmenity.Parking)),
                new ClinicMilestone("doctors.taxi.max", "Fully upgrade the taxi stand", 25,
                    (s, d) => d != null && d.Amenity(ClinicAmenity.Taxi)?.Level >= ClinicRules.MaximumAmenityLevel(d, ClinicAmenity.Taxi)),
                new ClinicMilestone("doctors.training.max", "Train a doctors clinic team member to the top level", 30,
                    (s, d) => d != null && d.Staff.Any(x => x.TrainingLevel >= ClinicRules.ComponentCap(d, ClinicRules.RoomForRole(x.Role)))),
                new ClinicMilestone("doctors.staff.full", "Fully staff the doctors clinic", 40,
                    (s, d) => d != null && Enum.GetValues(typeof(ClinicStaffRole)).Cast<ClinicStaffRole>().All(r => Count(d, r) >= ClinicRules.MaximumStaff(d, r)))
            };
            foreach (var room in new[] { ClinicRoom.Reception, ClinicRoom.FirstAid, ClinicRoom.Waiting })
                for (var tier = 2; tier <= 3; tier++)
                {
                    var kind = room; var target = tier;
                    list.Add(new ClinicMilestone("starter." + Key(kind) + ".tier" + target, "Renovate " + Name(kind) + " to tier " + target,
                        target == 2 ? 10 : 15, (s, d) => d != null || (s.Room(kind)?.Built == true && s.Room(kind).Tier >= target)));
                }
            foreach (var amenity in new[] { ClinicAmenity.Parking, ClinicAmenity.Toilet, ClinicAmenity.Vending })
            {
                var kind = amenity;
                list.Add(new ClinicMilestone("amenity." + Key(kind) + ".open", "Open the " + Name(kind), 5, (s, d) => d != null || s.Amenity(kind)?.Level >= 1));
                list.Add(new ClinicMilestone("amenity." + Key(kind) + ".max", "Fully upgrade the " + Name(kind), 10, (s, d) => d != null || s.Amenity(kind)?.Level >= 3));
            }
            for (var tier = 4; tier <= 6; tier++)
            {
                var target = tier;
                list.Add(new ClinicMilestone("doctors.tier" + target, "Renovate every doctors clinic room to tier " + target, 10 * (target - 1),
                    (s, d) => d != null && d.Rooms.All(r => r.Built && r.Tier >= target)));
            }
            return list;
        }

        private static long Treatments(ClinicState s, ClinicState d) => s.TotalTreatments + (d?.TotalTreatments ?? 0);
        private static long Earned(ClinicState s, ClinicState d) => s.TotalEarned + (d?.TotalEarned ?? 0);
        private static int Count(ClinicState state, ClinicStaffRole role) => state.Staff.Count(x => x.Role == role);
        private static string Key(ClinicRoom room) => room == ClinicRoom.FirstAid ? "firstaid" : room.ToString().ToLowerInvariant();
        private static string Key(ClinicAmenity amenity) => amenity.ToString().ToLowerInvariant();
        private static string Name(ClinicRoom room) => room == ClinicRoom.FirstAid ? "first aid" : room == ClinicRoom.Waiting ? "the waiting room" : room.ToString().ToLowerInvariant();
        private static string Name(ClinicAmenity amenity) => amenity == ClinicAmenity.Vending ? "vending machine" : amenity.ToString().ToLowerInvariant();
    }
}
