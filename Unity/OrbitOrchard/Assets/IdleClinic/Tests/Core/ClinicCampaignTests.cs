using System;
using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>A whole campaign under the current rules: every level of both clinics, earned through play.</summary>
    public sealed class ClinicCampaignTests
    {
        /// <summary>Play time to finish each clinic, earned through ordinary play and collection.</summary>
        [Test] public void TheNewRulesFinishTheStarterClinicSoonerAndKeepTheDoctorsClinicALongGoal()
        {
            double Hours(ClinicState state) => state.Tick / (double)ClinicRules.TicksPerSecond / 3600;
            var starter3 = Hours(DoctorsProgressionFixture.MaxStarter(3).State);
            var starter4 = Hours(DoctorsProgressionFixture.MaxStarter(4).State);
            var doctors3 = Hours(DoctorsProgressionFixture.MaxDoctors(3).State);
            var doctors4 = Hours(DoctorsProgressionFixture.MaxDoctors(4).State);
            TestContext.WriteLine($"Starter clinic complete: rules 3 {starter3:0.0}h, rules 4 {starter4:0.0}h. Doctors clinic maxed: rules 3 {doctors3:0.0}h, rules 4 {doctors4:0.0}h.");
            Assert.That(starter4, Is.LessThan(starter3), "The first clinic is quicker to finish.");
            Assert.That(doctors4, Is.GreaterThan(4), "The doctors clinic stays a long-term goal.");
        }

        [Test] public void EveryLevelOfBothClinicsIsReachableAndEveryGoalCanBeEarned()
        {
            var starter = DoctorsProgressionFixture.MaxStarter();
            var s = starter.State;
            Assert.That(s.RulesVersion, Is.EqualTo(ClinicBalance.CurrentRulesVersion));
            Assert.That(ClinicRules.StarterCompletion(s), Is.Empty, "Every starter room, track, desk, station, staff member and amenity is maxed.");
            Assert.That(s.TotalParkingFees, Is.GreaterThan(0), "Parked cars paid at the barrier along the way.");
            Assert.That(ClinicSimulation.IsValidState(s), Is.True);
            Assert.That(starter.UnlockDoctorsClinic().Success, Is.True);
            Assert.That(ClinicSimulation.IsValidState(s), Is.True);

            var doctors = DoctorsProgressionFixture.MaxDoctors();
            var d = doctors.State;
            Assert.That(d.RulesVersion, Is.EqualTo(ClinicBalance.CurrentRulesVersion));
            foreach (var room in d.Rooms)
            {
                Assert.That(room.Tier, Is.EqualTo(ClinicRules.MaximumTier(d)), room.Kind + " tier");
                foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
                    Assert.That(room.Level(track), Is.EqualTo(ClinicRules.ComponentCap(d, room.Kind)), room.Kind + " " + track);
            }
            foreach (ClinicStaffRole role in Enum.GetValues(typeof(ClinicStaffRole)))
            {
                Assert.That(d.Staff.Count(x => x.Role == role), Is.EqualTo(ClinicRules.MaximumStaff(d, role)), role + " staff");
                Assert.That(ClinicRules.StationCount(d, role), Is.EqualTo(ClinicRules.MaximumStaff(d, role)), role + " stations");
            }
            foreach (var staff in d.Staff)
            {
                var cap = ClinicRules.ComponentCap(d, ClinicRules.RoomForRole(staff.Role));
                Assert.That(staff.TrainingLevel, Is.EqualTo(cap));
                Assert.That(ClinicRules.StationLevel(d, staff.Role, staff.StationId), Is.EqualTo(cap));
            }
            foreach (var amenity in d.Amenities)
                Assert.That(amenity.Level, Is.EqualTo(ClinicRules.MaximumAmenityLevel(d, amenity.Kind)), amenity.Kind.ToString());
            Assert.That(ClinicSimulation.IsValidState(d), Is.True);

            // Keep both clinics running until every goal is met: none is out of reach.
            for (var day = 0; day < 160 && ClinicMilestones.All.Any(m => !m.IsMet(s, d)); day++)
            {
                starter.AdvanceOffline(8 * 3600); doctors.AdvanceOffline(8 * 3600);
                foreach (var game in new[] { starter, doctors })
                {
                    foreach (var desk in game.State.ReceptionDesks) game.Collect(desk.Id);
                    game.CollectVendingTips(); game.CollectParkingFees();
                    Assert.That(ClinicSimulation.IsValidState(game.State), Is.True, "Valid after day " + day);
                }
            }
            Assert.That(ClinicMilestones.All.Where(m => !m.IsMet(s, d)).Select(m => m.Id), Is.Empty);
            // Every guide step is reachable too, including the doctors clinic's.
            var progress = new ClinicGuideProgress(1, 1);
            Assert.That(ClinicGuide.Steps.Where(g => !g.IsDone(s, d, progress)).Select(g => g.Id), Is.Empty);
            Assert.That(d.TotalParkingFees, Is.GreaterThan(0));
            var till = d.Amenity(ClinicAmenity.Parking).Till;
            if (till > 0) Assert.That(doctors.CollectParkingFees().Amount, Is.EqualTo(till));
            Assert.That(d.Amenity(ClinicAmenity.Parking).Till, Is.Zero);
            Assert.That(ClinicSimulation.IsValidState(d), Is.True);
        }
    }
}
