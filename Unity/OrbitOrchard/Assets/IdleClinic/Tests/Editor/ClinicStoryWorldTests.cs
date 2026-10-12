using System.Linq;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>Maya in the clinic: she walks up the pavement to the door, steps inside once the prologue is over, and is hired
    /// where she stands, keeping her look. Games without the story keep the first nurse exactly as before.</summary>
    public sealed class ClinicStoryWorldTests
    {
        private static readonly Vector3 Door = new Vector3(.67f, .14f, -6.02f), Inside = new Vector3(-.30f, .14f, -4.70f);
        private static ClinicStoryCast Cast(bool arrived = false, bool inside = false) => new ClinicStoryCast
            { Enabled = true, Arrived = arrived, Inside = inside, StaffId = ClinicStory.FirstNurseStaffId, Appearance = ClinicStory.FirstNurseAppearance };

        private static Transform Find(GameObject host, string name) => host.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        private static float Flat(Vector3 a, Vector3 b) { var d = a - b; d.y = 0; return d.magnitude; }
        private static void Play(ClinicSimulation sim, ClinicWorld world, int frames) { for (var i = 0; i < frames; i++) { sim.Advance(.1); world.Render(sim.State, .1f, true); } }

        /// <summary>Play the opening up to <paramref name="target"/> as taps would: start each task, collect the first payment.</summary>
        private static void PlayOpeningTo(ClinicSimulation sim, ClinicOpeningStep target)
        {
            for (var guard = 0; guard < 4000 && sim.State.OpeningStep < target; guard++)
            {
                var step = sim.State.OpeningStep;
                if (sim.State.OpeningTaskEndsTick < 0 && ClinicSimulation.OpeningManual(step)) sim.ContinueOpening(step);
                sim.Advance(.5);
                foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
            }
            Assert.That(sim.State.OpeningStep, Is.EqualTo(target));
        }

        [Test]
        public void MayaWalksUpToTheDoorStepsInsideAndIsHiredWhereSheStands()
        {
            var host = new GameObject("Maya test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast());
                world.Render(sim.State, 0, true);
                var maya = Find(host, "Maya");
                Assert.That(maya, Is.Not.Null);
                Assert.That(maya.gameObject.activeInHierarchy, Is.True, "A new game shows Maya from the first frame.");
                Assert.That(maya.position.z, Is.LessThan(-6.3f), "She starts on the pavement.");
                Play(sim, world, 80);
                Assert.That(Flat(maya.position, Door), Is.LessThan(.05f), "She waits at the door.");
                Assert.That(world.TryGetStoryNursePoint(out var head), Is.True);
                Assert.That(head.y, Is.GreaterThan(1.8f), "The speech bubble sits above her head.");

                var doors = world.DoorOpeningCount;
                world.SetStoryCast(Cast(true, true));
                Play(sim, world, 40);
                Assert.That(world.DoorOpeningCount, Is.GreaterThan(doors), "The entrance opens for her.");
                Assert.That(Flat(maya.position, Inside), Is.LessThan(.05f), "She waits just inside, clear of the corridor.");

                PlayOpeningTo(sim, ClinicOpeningStep.HireNurse);
                world.Render(sim.State, .1f, true);
                var waiting = maya.position;
                Assert.That(Flat(waiting, Inside), Is.LessThan(.05f));
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.HireNurse).Success, Is.True);
                Play(sim, world, 1);
                var hired = Find(host, "Staff " + ClinicStory.FirstNurseStaffId);
                Assert.That(hired, Is.Not.Null);
                Assert.That(hired, Is.SameAs(maya), "The same figure walks on to her station.");
                Assert.That(Find(host, "Maya"), Is.Null, "No second Maya stays behind.");
                Assert.That(Flat(hired.position, waiting), Is.LessThan(.3f), "Hired where she stands, not at the door.");
                var materials = hired.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Select(m => m.name).ToArray();
                Assert.That(materials, Does.Contain("Clinic HairChestnut"), "Maya keeps her chestnut hair.");
                Play(sim, world, 60);
                Assert.That(Flat(hired.position, world.GetAnchorPoint(ClinicRules.StationStaffAnchor(ClinicStaffRole.Nurse, 0))), Is.LessThan(.1f),
                    "She reaches the first nursing station.");
                Assert.That(world.TryGetStoryNursePoint(out var atStation), Is.True, "The bubble follows her to her station.");
                Assert.That(Flat(atStation, hired.position), Is.LessThan(.01f));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void ARelaunchFindsMayaWhereTheStoryLeftHer()
        {
            var host = new GameObject("Maya relaunch test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast(arrived: true));
                world.Render(sim.State, 0, true);
                Assert.That(Flat(Find(host, "Maya").position, Door), Is.LessThan(.05f), "Mid-prologue: already at the door.");
                Object.DestroyImmediate(host);
                host = new GameObject("Maya relaunch test 2");
                world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast(true, true));
                world.Render(sim.State, 0, true);
                Assert.That(Flat(Find(host, "Maya").position, Inside), Is.LessThan(.05f), "After the prologue: already inside.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void WithoutTheStoryNobodyWaitsAtTheDoorAndTheFirstNurseLooksAsBefore()
        {
            var host = new GameObject("No story test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.Render(sim.State, 0, true);
                Assert.That(Find(host, "Maya"), Is.Null);
                Assert.That(world.TryGetStoryNursePoint(out _), Is.False);
                PlayOpeningTo(sim, ClinicOpeningStep.HireNurse);
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.HireNurse).Success, Is.True);
                Play(sim, world, 2);
                var nurse = Find(host, "Staff " + ClinicStory.FirstNurseStaffId);
                var materials = nurse.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Select(m => m.name).ToArray();
                Assert.That(materials, Does.Contain("Clinic HairGold"), "Older games keep their first nurse's look.");
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
