using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleClinic.Core
{
    public enum ClinicGuideFocus { None, Room, Amenity, Gems, Locations }

    /// <summary>Progress the guide can see beyond the clinics themselves.</summary>
    public readonly struct ClinicGuideProgress
    {
        public int GoalsClaimed { get; }
        public long GemsSpent { get; }
        public ClinicGuideProgress(int goalsClaimed, long gemsSpent) { GoalsClaimed = goalsClaimed; GemsSpent = gemsSpent; }
    }

    public sealed class ClinicGuideStep
    {
        public string Id { get; }
        public string Title { get; }
        public string Why { get; }
        public long GemReward { get; }
        public ClinicGuideFocus Focus { get; }
        public ClinicRoom Room { get; }
        public ClinicAmenity Amenity { get; }
        /// <summary>The dock control to highlight once the focused room or object is open.</summary>
        public string Control { get; }
        /// <summary>The clinic where this step happens; the guide sends players there first.</summary>
        public ClinicLocation Location { get; }
        private readonly Func<ClinicState, ClinicState, ClinicGuideProgress, bool> done;

        internal ClinicGuideStep(string id, string title, string why, long gems, ClinicGuideFocus focus, Func<ClinicState, ClinicState, ClinicGuideProgress, bool> done,
            ClinicRoom room = ClinicRoom.Reception, ClinicAmenity amenity = ClinicAmenity.Parking, string control = null,
            ClinicLocation location = ClinicLocation.StarterClinic)
        { Id = id; Title = title; Why = why; GemReward = gems; Focus = focus; Room = room; Amenity = amenity; Control = control; Location = location; this.done = done; }

        public bool IsDone(ClinicState starter, ClinicState doctors, ClinicGuideProgress progress) => starter != null && done(starter, doctors, progress);
    }

    /// <summary>A short guided path after the opening tutorial: one clear next action at a time, each teaching a
    /// feature and paying a few gems. Ids are stored with claimed goals: never rename or reuse one.</summary>
    public static class ClinicGuide
    {
        public const string Prefix = "guide.";

        public static IReadOnlyList<ClinicGuideStep> Steps { get; } = new List<ClinicGuideStep>
        {
            new ClinicGuideStep("guide.equipment", "Upgrade first aid equipment", "Faster care treats more patients every minute.", 5,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Room(ClinicRoom.FirstAid).EquipmentLevel >= 2, ClinicRoom.FirstAid, control: "upgrade-firstaid-equipment"),
            new ClinicGuideStep("guide.waiting", "Build the waiting room", "Seats stop the queue spilling into the street.", 5,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Room(ClinicRoom.Waiting).Built, ClinicRoom.Waiting, control: "build-waiting-room"),
            new ClinicGuideStep("guide.decor", "Decorate reception", "Decor is optional and bought with gems. Each level adds 5% to every visit fee.", 5,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Room(ClinicRoom.Reception).DecorationLevel >= 2, ClinicRoom.Reception, control: "upgrade-reception-decoration"),
            new ClinicGuideStep("guide.renovate", "Renovate first aid", "Renovations raise upgrade limits and make room for another station.", 5,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Room(ClinicRoom.FirstAid).Tier >= 2, ClinicRoom.FirstAid, control: "expand-room"),
            new ClinicGuideStep("guide.nurse", "Hire a second nurse", "Two nurses treat twice as many patients.", 10,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Staff.Count(x => x.Role == ClinicStaffRole.Nurse) >= 2, ClinicRoom.FirstAid),
            new ClinicGuideStep("guide.parking", "Open the car park", "Drivers pay at the barrier as they leave.", 5,
                ClinicGuideFocus.Amenity, (s, d, p) => d != null || s.Amenity(ClinicAmenity.Parking).Level >= 1, amenity: ClinicAmenity.Parking, control: "build-car-park"),
            new ClinicGuideStep("guide.receptionist", "Hire a second receptionist", "A second desk checks in two patients at once.", 10,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Staff.Count(x => x.Role == ClinicStaffRole.Receptionist) >= 2, ClinicRoom.Reception, control: "hire-receptionist"),
            new ClinicGuideStep("guide.train", "Train a member of staff", "Tap a desk or treatment chair. Trained staff work faster.", 5,
                ClinicGuideFocus.Room, (s, d, p) => d != null || s.Staff.Any(x => x.TrainingLevel >= 2), ClinicRoom.FirstAid),
            new ClinicGuideStep("guide.vending", "Add a vending machine", "Waiting patients leave tips.", 5,
                ClinicGuideFocus.Amenity, (s, d, p) => d != null || s.Amenity(ClinicAmenity.Vending).Level >= 1, amenity: ClinicAmenity.Vending, control: "build-vending-machine"),
            new ClinicGuideStep("guide.goals", "Collect a goal reward", "Tap the gem counter to see every goal.", 5,
                ClinicGuideFocus.Gems, (s, d, p) => p.GoalsClaimed >= 1),
            new ClinicGuideStep("guide.finish", "Finish a renovation with gems", "Start a renovation, then tap Finish beside its timer.", 5,
                ClinicGuideFocus.Room, (s, d, p) => p.GemsSpent > 0, ClinicRoom.Reception, control: "expand-room"),
            new ClinicGuideStep("guide.doctors", "Open the doctors clinic", "Complete every starter upgrade, then open it from your clinics.", 25,
                ClinicGuideFocus.Locations, (s, d, p) => d != null),
            // The doctors clinic: consultations, the pharmacy, taxis and the climb to tier 6.
            Doctors("guide.doctors.consult", "Upgrade consultation equipment", "Faster consultations move patients on to the pharmacy.", 10,
                ClinicGuideFocus.Room, d => d.Room(ClinicRoom.Consultation).EquipmentLevel >= 2, ClinicRoom.Consultation, control: "upgrade-consultation-equipment"),
            Doctors("guide.doctors.pharmacy", "Upgrade pharmacy equipment", "Quicker dispensing clears the queue for new patients.", 10,
                ClinicGuideFocus.Room, d => d.Room(ClinicRoom.Pharmacy).EquipmentLevel >= 2, ClinicRoom.Pharmacy, control: "upgrade-pharmacy-equipment"),
            Doctors("guide.doctors.taxi", "Open the taxi stand", "Taxi patients don't need a parking space.", 10,
                ClinicGuideFocus.Amenity, d => d.Amenity(ClinicAmenity.Taxi)?.Level >= 1, amenity: ClinicAmenity.Taxi, control: "build-taxi-stand"),
            Doctors("guide.doctors.doctor", "Hire a second doctor", "Add a consultation room, then hire a doctor for it.", 15,
                ClinicGuideFocus.Room, d => d.Staff.Count(x => x.Role == ClinicStaffRole.Doctor) >= 2, ClinicRoom.Consultation),
            Doctors("guide.doctors.tier3", "Renovate consultations to room 3", "Bigger rooms raise every upgrade limit.", 15,
                ClinicGuideFocus.Room, d => d.Room(ClinicRoom.Consultation).Tier >= 3, ClinicRoom.Consultation, control: "expand-room"),
            Doctors("guide.doctors.parking", "Grow the car park to level 3", "More bays, and every car pays more at the barrier.", 15,
                ClinicGuideFocus.Amenity, d => d.Amenity(ClinicAmenity.Parking)?.Level >= 3, amenity: ClinicAmenity.Parking, control: "upgrade-car-park"),
            Doctors("guide.doctors.train", "Train a doctor to level 4", "Tap a consultation desk. Training speeds up every visit.", 15,
                ClinicGuideFocus.Room, d => d.Staff.Any(x => x.Role == ClinicStaffRole.Doctor && x.TrainingLevel >= 4), ClinicRoom.Consultation),
            Doctors("guide.doctors.tier4", "Renovate any room to tier 4", "Tier 4 opens the next set of improvements.", 20,
                ClinicGuideFocus.Room, d => d.Rooms.Any(r => r.Tier >= 4), ClinicRoom.Reception, control: "expand-room"),
            Doctors("guide.doctors.team", "Fully staff the doctors clinic", "Four receptionists, doctors and nurses, and two pharmacists.", 25,
                ClinicGuideFocus.Room, d => Enum.GetValues(typeof(ClinicStaffRole)).Cast<ClinicStaffRole>().All(r => d.Staff.Count(x => x.Role == r) >= ClinicRules.MaximumStaff(d, r)), ClinicRoom.Reception),
            Doctors("guide.doctors.tier6", "Renovate any room to tier 6", "The finest room in town. Keep going for every goal.", 40,
                ClinicGuideFocus.Room, d => d.Rooms.Any(r => r.Tier >= 6), ClinicRoom.Reception, control: "expand-room")
        };

        private static ClinicGuideStep Doctors(string id, string title, string why, long gems, ClinicGuideFocus focus, Func<ClinicState, bool> done,
            ClinicRoom room = ClinicRoom.Reception, ClinicAmenity amenity = ClinicAmenity.Parking, string control = null)
            => new ClinicGuideStep(id, title, why, gems, focus, (s, d, p) => d != null && done(d), room, amenity, control, ClinicLocation.DoctorsClinic);

        public static ClinicGuideStep Find(string id) => Steps.FirstOrDefault(s => s.Id == id);
    }
}
