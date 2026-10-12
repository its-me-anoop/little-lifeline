using System.Linq;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleClinic.Tests
{
    /// <summary>Maya and the boss in the clinic: she walks up to the door, taps on the glass and meets the boss inside the
    /// lobby; after the prologue she keeps a step behind the boss through the opening and stands beside the boss in each
    /// room, is hired where she stands, and the boss stays with her until the clinic opens. While they talk the camera holds a
    /// close view of the two of them with nothing built in the way. Games without the story keep the first nurse exactly as
    /// before, and the boss never jumps between places.</summary>
    public sealed class ClinicStoryWorldTests
    {
        private static readonly Vector3 Inside = new Vector3(.45f, .14f, -3.15f);
        private static ClinicStoryCast Cast(bool arrived = false, bool greeting = true, bool together = true) => new ClinicStoryCast
        {
            Enabled = true, Arrived = arrived, Greeting = greeting, KeepTogether = together,
            StaffId = ClinicStory.FirstNurseStaffId, Appearance = ClinicStory.FirstNurseAppearance
        };

        private static Transform Find(GameObject host, string name) => host.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        private static float Flat(Vector3 a, Vector3 b) { var d = a - b; d.y = 0; return d.magnitude; }
        private static Vector3 BossPoint(int zone) { var p = ClinicBossRoute.Point(zone); return new Vector3((float)p.X, .14f, (float)p.Z); }
        private static void Play(ClinicSimulation sim, ClinicWorld world, int frames) { for (var i = 0; i < frames; i++) { sim.Advance(.1); world.Render(sim.State, .1f, false); } }

        /// <summary>Play the opening up to <paramref name="target"/> as taps would: start each task, collect the first payment.</summary>
        private static void PlayOpeningTo(ClinicSimulation sim, ClinicOpeningStep target)
        {
            for (var guard = 0; guard < 4000 && sim.State.OpeningStep < target; guard++)
            {
                var step = sim.State.OpeningStep;
                if (sim.State.OpeningTaskEndsTick < 0 && (ClinicSimulation.OpeningManual(step) || step == ClinicOpeningStep.HireNurse || step == ClinicOpeningStep.HireReceptionist))
                    sim.ContinueOpening(step);
                sim.Advance(.5);
                foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
            }
            Assert.That(sim.State.OpeningStep, Is.EqualTo(target));
        }

        [Test]
        public void MayaKnocksMeetsTheBossInsideAndTheyWalkTogether()
        {
            var host = new GameObject("Maya test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast());
                world.Render(sim.State, 0, false);
                var maya = Find(host, "Maya"); var boss = Find(host, "Clinic boss");
                Assert.That(maya.gameObject.activeInHierarchy, Is.True, "A new game shows Maya from the first frame.");
                Assert.That(maya.position.z, Is.LessThan(-6.3f), "She starts on the pavement.");
                Assert.That(Flat(boss.position, BossPoint(ClinicBossRoute.Entrance)), Is.LessThan(.05f), "The boss waits just inside the door.");
                var doors = world.DoorOpeningCount;
                Play(sim, world, 110);
                Assert.That(world.DoorOpeningCount, Is.GreaterThan(doors), "The door opens for her.");
                Assert.That(Flat(maya.position, Inside), Is.LessThan(.05f), "She comes in to the boss.");
                Assert.That(Flat(maya.position, boss.position), Is.LessThan(1.2f), "Together while they talk.");
                var toward = maya.position - boss.position; toward.y = 0;
                Assert.That(Vector3.Dot(boss.forward, toward.normalized), Is.GreaterThan(.9f), "The boss turns to her.");
                Assert.That(Vector3.Dot(maya.forward, -toward.normalized), Is.GreaterThan(.9f), "She turns to the boss.");

                // "My office." They set off together; she keeps a step behind all the way.
                world.SetStoryCast(Cast(true, greeting: false));
                Assert.That(sim.StartOpeningAtEntrance().Success, Is.True);
                float widest = 0, fastest = 0; var previous = boss.position;
                for (var frame = 0; frame < 90; frame++)
                {
                    Play(sim, world, 1);
                    if (frame > 15) widest = Mathf.Max(widest, Flat(maya.position, boss.position));
                    fastest = Mathf.Max(fastest, Flat(previous, boss.position) / .1f); previous = boss.position;
                }
                Assert.That(widest, Is.LessThan(2.2f), "Never far apart on the way.");
                Assert.That(fastest, Is.LessThanOrEqualTo((float)ClinicBossRoute.MetresPerSecond + .05f), "The boss walks; nobody jumps.");
                Assert.That(Flat(boss.position, BossPoint(3)), Is.LessThan(.05f), "In the office.");
                Assert.That(Flat(maya.position, boss.position), Is.InRange(.6f, 1.3f), "Beside the boss.");
                Assert.That(Mathf.Abs(Vector3.Dot(world.SceneCamera.transform.right, maya.position - boss.position)), Is.GreaterThan(.7f),
                    "Side by side on screen, not one behind the other in the doorway.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void SheIsHiredBesideTheBossWhoStaysWithHerUntilTheClinicOpens()
        {
            var host = new GameObject("Maya hire test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast(true, greeting: false));
                PlayOpeningTo(sim, ClinicOpeningStep.HireNurse);
                world.Render(sim.State, 0, false);
                Play(sim, world, 20);
                var maya = Find(host, "Maya"); var boss = Find(host, "Clinic boss");
                Assert.That(Flat(boss.position, BossPoint(1)), Is.LessThan(.05f), "The boss set up the nursing room.");
                Assert.That(Flat(maya.position, boss.position), Is.LessThan(1.3f), "She is right there for the job offer.");
                var waiting = maya.position;
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.HireNurse).Success, Is.True);
                Play(sim, world, 1);
                var hired = Find(host, "Staff " + ClinicStory.FirstNurseStaffId);
                Assert.That(hired, Is.SameAs(maya), "The same figure walks on to her station.");
                Assert.That(Find(host, "Maya"), Is.Null, "No second Maya stays behind.");
                Assert.That(Flat(hired.position, waiting), Is.LessThan(.3f), "Hired where she stands.");
                Assert.That(hired.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Select(m => m.name), Does.Contain("Clinic HairChestnut"));
                Play(sim, world, 60);
                Assert.That(Flat(hired.position, world.GetAnchorPoint(ClinicRules.StationStaffAnchor(ClinicStaffRole.Nurse, 0))), Is.LessThan(.1f), "At nursing station 1.");
                Assert.That(Flat(boss.position, BossPoint(1)), Is.LessThan(.05f), "The boss stays with her while the first patients line up.");
                Assert.That(world.TryGetStoryNursePoint(out var head), Is.True);
                Assert.That(Flat(head, hired.position), Is.LessThan(.01f), "Her speech bubble follows her to her station.");

                // The first patients queue; hiring the receptionist opens the clinic. Watched frame by frame.
                var previous = boss.position; float fastest = 0;
                for (var frame = 0; frame < 1200 && sim.State.OpeningStep != ClinicOpeningStep.Complete; frame++)
                {
                    if (sim.State.OpeningStep == ClinicOpeningStep.HireReceptionist) sim.ContinueOpening(ClinicOpeningStep.HireReceptionist);
                    Play(sim, world, 1); fastest = Mathf.Max(fastest, Flat(previous, boss.position) / .1f); previous = boss.position;
                }
                Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.Complete));
                world.SetStoryCast(Cast(true, greeting: false, together: false));
                for (var frame = 0; frame < 120; frame++) { Play(sim, world, 1); fastest = Mathf.Max(fastest, Flat(previous, boss.position) / .1f); previous = boss.position; }
                Assert.That(Flat(boss.position, BossPoint(3)), Is.LessThan(.05f), "Once the clinic is open the boss walks back to the office.");
                Assert.That(fastest, Is.LessThanOrEqualTo((float)ClinicBossRoute.MetresPerSecond + .05f), "Walked, not jumped.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void ARelaunchFindsThemWhereTheStoryLeftThem()
        {
            var host = new GameObject("Maya relaunch test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast(arrived: true));
                world.Render(sim.State, 0, false);
                Assert.That(Flat(Find(host, "Maya").position, Inside), Is.LessThan(.05f), "Mid-prologue: inside with the boss.");
                Assert.That(Flat(Find(host, "Clinic boss").position, BossPoint(ClinicBossRoute.Entrance)), Is.LessThan(.05f));
                Object.DestroyImmediate(host);
                PlayOpeningTo(sim, ClinicOpeningStep.CleanWaiting);
                host = new GameObject("Maya relaunch test 2");
                world = host.AddComponent<ClinicWorld>();
                world.SetStoryCast(Cast(true, greeting: false));
                world.Render(sim.State, 0, false);
                Assert.That(Flat(Find(host, "Maya").position, Find(host, "Clinic boss").position), Is.LessThan(1.3f), "After the prologue: beside the boss.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void WhileTheyTalkTheCameraHoldsACloseViewOfBothThenReturnsToTheClinic()
        {
            var host = new GameObject("Story camera test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.SetRenderSize(393, 852);
                world.SetStoryCast(Cast(arrived: true));
                world.Render(sim.State, 0, false);
                var home = world.SceneCamera.orthographicSize;
                world.FollowStory(1.9f, .63f, .46f, true);
                Play(sim, world, 30);
                Assert.That(world.SceneCamera.orthographicSize, Is.LessThan(home * .45f), "A close view: the two of them fill the picture.");
                foreach (var name in new[] { "Maya", "Clinic boss" })
                {
                    var feet = world.WorldToViewport(Find(host, name).position);
                    var head = world.WorldToViewport(Find(host, name).position + Vector3.up * 1.8f);
                    Assert.That(feet.x, Is.InRange(.05f, .95f), name);
                    Assert.That(feet.y, Is.GreaterThan(.36f), name + " stands clear of the guide card.");
                    Assert.That(head.y, Is.LessThan(.90f), name + " stays below the balances.");
                }
                world.Pan(new Vector2(.5f, .5f), new Vector2(.7f, .3f));
                var panned = world.SceneCamera.transform.position;
                Play(sim, world, 10);
                Assert.That(world.SceneCamera.transform.position, Is.EqualTo(panned), "Looking around pauses the close view.");
                world.ResumeStoryFollow();
                Play(sim, world, 30);
                Assert.That(world.SceneCamera.orthographicSize, Is.LessThan(home * .45f), "A new line brings it back.");
                world.StopStoryFollow();
                Play(sim, world, 30);
                Assert.That(world.SceneCamera.orthographicSize, Is.EqualTo(home).Within(.05f), "Back to the whole clinic.");
                Assert.That(world.FollowingStory, Is.False);
            }
            finally { Object.DestroyImmediate(host); }
        }

        /// <summary>The bug the first close-ups had: from the high camera the shop front's sign hid both of them at the door, and
        /// the office's front wall hid Maya. Being inside the picture is not enough; every point of each body, from the shins
        /// to the face and both sides, must have nothing built between it and the camera.</summary>
        [Test]
        public void NothingBuiltStandsBetweenTheCameraAndEitherOfThemWhileTheyTalk()
        {
            // At the door, for the first lines and the name question: they meet well inside, so the shop front stays whole.
            var sim = ClinicSimulation.CreateOpening();
            Look(sim, Cast(arrived: true), false, host =>
                Assert.That(Cut(host), Is.Empty, "At the door nothing needs cutting away: the name sign stays up."));
            // In the office the front wall is cut away while they talk there.
            sim = ClinicSimulation.CreateOpening(); PlayOpeningTo(sim, ClinicOpeningStep.SetUpOffice);
            Look(sim, Cast(true, greeting: false), false, host => Assert.That(Cut(host), Does.Contain("Office front left")));
            // The first check-in, the job offer in the nursing room and the first queue.
            foreach (var step in new[] { ClinicOpeningStep.FirstCheckIn, ClinicOpeningStep.HireNurse, ClinicOpeningStep.QueueArrivals })
            {
                sim = ClinicSimulation.CreateOpening(); PlayOpeningTo(sim, step);
                Look(sim, Cast(true, greeting: false), step == ClinicOpeningStep.QueueArrivals, null);
            }
        }

        /// <summary>A fresh clinic at this point of the story with the conversation view on: both in the picture with nothing
        /// in the way, then everything back as soon as the player looks around and when the conversation ends.</summary>
        private static void Look(ClinicSimulation sim, ClinicStoryCast cast, bool queue, System.Action<GameObject> also)
        {
            var host = new GameObject("Story view test");
            try
            {
                var moment = sim.State.OpeningStep + (cast.Greeting ? " (prologue)" : "");
                var world = host.AddComponent<ClinicWorld>();
                world.SetRenderSize(393, 852);
                world.SetStoryCast(cast);
                world.Render(sim.State, 0, false);
                world.FollowStory(2.4f, queue ? .60f : .63f, queue ? .56f : .46f, true, queue);
                Play(sim, world, 25);
                var boss = Find(host, "Clinic boss");
                var nurse = Find(host, "Maya") ?? Find(host, "Staff " + ClinicStory.FirstNurseStaffId);
                Assert.That(nurse, Is.Not.Null, moment);
                foreach (var person in new[] { boss, nurse })
                {
                    foreach (var height in new[] { 0f, 1.8f })
                    {
                        var seen = world.WorldToViewport(person.position + Vector3.up * height);
                        Assert.That(seen.x, Is.InRange(0f, 1f), moment + ": " + person.name + " in the picture.");
                        Assert.That(seen.y, Is.InRange(0f, 1f), moment + ": " + person.name + " in the picture.");
                    }
                    Assert.That(InTheWay(host, world, person), Is.Empty, moment + ": nothing in front of " + person.name + ".");
                }
                also?.Invoke(host);
                world.Pan(new Vector2(.5f, .5f), new Vector2(.55f, .45f));
                Play(sim, world, 1);
                Assert.That(Cut(host), Is.Empty, moment + ": looking around puts every wall back.");
                world.ResumeStoryFollow();
                Play(sim, world, 3);
                world.StopStoryFollow();
                Play(sim, world, 1);
                Assert.That(Cut(host), Is.Empty, moment + ": the end of the conversation puts every wall back.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        private static bool IsPerson(Transform part)
        {
            for (var node = part; node != null; node = node.parent)
                if (node.name == "Maya" || node.name == "Clinic boss" || node.name.StartsWith("Staff ") || node.name.StartsWith("Patient ") || node.name.StartsWith("Turned-away patient"))
                    return true;
            return false;
        }

        /// <summary>Pieces drawn only as shadow for the conversation (cut away).</summary>
        private static string[] Cut(GameObject host) =>
            host.GetComponentsInChildren<MeshRenderer>().Where(r => r.shadowCastingMode == ShadowCastingMode.ShadowsOnly).Select(r => r.name).ToArray();

        /// <summary>Everything drawn (opaque, not a person, not cut away) that stands between the camera and this person's
        /// shins, knees, hips, chest or face, or either side of the body.</summary>
        private static string[] InTheWay(GameObject host, ClinicWorld world, Transform person)
        {
            var toCamera = -world.SceneCamera.transform.forward; var right = world.SceneCamera.transform.right;
            var points = new[] { .35f, .7f, 1.05f, 1.4f, 1.7f }.Select(h => person.position + Vector3.up * h)
                .Concat(new[] { 1f, 1.4f }.SelectMany(h => new[] { -.14f, .14f }.Select(side => person.position + Vector3.up * h + right * side)))
                .ToArray();
            return host.GetComponentsInChildren<MeshRenderer>()
                .Where(r => r.enabled && (r.shadowCastingMode == ShadowCastingMode.On || r.shadowCastingMode == ShadowCastingMode.TwoSided))
                .Where(r => r.sharedMaterial == null || r.sharedMaterial.renderQueue <= (int)RenderQueue.GeometryLast)
                .Where(r => !IsPerson(r.transform))
                .Where(r => points.Any(point => r.bounds.IntersectRay(new Ray(point, toCamera))))
                .Select(r => r.name + " " + r.bounds.center.ToString("F2"))
                .ToArray();
        }

        [Test]
        public void WithoutTheStoryNobodyWaitsAtTheDoorTheFirstNurseLooksAsBeforeAndTheBossWalks()
        {
            var host = new GameObject("No story test");
            try
            {
                var sim = ClinicSimulation.CreateOpening();
                var world = host.AddComponent<ClinicWorld>();
                world.Render(sim.State, 0, false);
                Assert.That(Find(host, "Maya"), Is.Null);
                Assert.That(world.TryGetStoryNursePoint(out _), Is.False);
                Assert.That(Flat(Find(host, "Clinic boss").position, BossPoint(3)), Is.LessThan(.05f), "The boss starts in the office as before.");
                PlayOpeningTo(sim, ClinicOpeningStep.HireNurse);
                world.Render(sim.State, 0, false);
                var boss = Find(host, "Clinic boss");
                Assert.That(sim.ContinueOpening(ClinicOpeningStep.HireNurse).Success, Is.True);
                var previous = boss.position; float fastest = 0;
                for (var frame = 0; frame < 120; frame++) { Play(sim, world, 1); fastest = Mathf.Max(fastest, Flat(previous, boss.position) / .1f); previous = boss.position; }
                Assert.That(fastest, Is.LessThanOrEqualTo((float)ClinicBossRoute.MetresPerSecond + .05f), "After the hire the boss walks to the office instead of jumping there.");
                Assert.That(Flat(boss.position, BossPoint(3)), Is.LessThan(.05f));
                var nurse = Find(host, "Staff " + ClinicStory.FirstNurseStaffId);
                Assert.That(nurse.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Select(m => m.name), Does.Contain("Clinic HairGold"),
                    "Older games keep their first nurse's look.");
            }
            finally { Object.DestroyImmediate(host); }
        }
    }
}
