using System.Linq;
using System.Reflection;
using IdleClinic.Core;
using IdleClinic.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>4.3 (23) character pass: one smoothly skinned body per model within a mobile budget, hair and trousers the
    /// game tints per person, and headwear, badges and the departure dressing that sit on the new head and torso.</summary>
    public sealed class ClinicCharacterTests
    {
        private GameObject host;
        private ClinicWorld world;
        [SetUp] public void SetUp() { host = new GameObject("Clinic character test"); world = host.AddComponent<ClinicWorld>(); world.Initialize(); }
        [TearDown] public void TearDown() => Object.DestroyImmediate(host);

        [TestCase("Patient")][TestCase("Nurse")][TestCase("Receptionist")]
        public void EachModelIsOneSkinnedBodyWithTintableHairAndTrousersWithinAMobileBudget(string name)
        {
            var model = Resources.Load<GameObject>("Clinic/Models/" + name);
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            Assert.That(skins.Length, Is.EqualTo(1), "One skinned mesh, so the whole body bends together.");
            var names = skins[0].sharedMaterials.Select(m => m.name).ToArray();
            foreach (var role in new[] { "Skin", "Hair", "Trousers", "Lip" }) Assert.That(names, Does.Contain(role), role);
            Assert.That(names.Length, Is.LessThanOrEqualTo(10), "Each material slot is a draw call on the phone.");
            Assert.That(skins[0].sharedMesh.vertexCount, Is.LessThan(6500));
            Assert.That(skins[0].bones.Select(b => b.name), Does.Contain("head").And.Contain("foot.L").And.Contain("hand.R"));
        }

        [Test]
        public void StyleDataCoversEveryAppearanceWithColoursTheGameKnows()
        {
            var styles = ClinicCharacterStyles.Current;
            Assert.That(styles.hairRoles.Length, Is.EqualTo(12));
            Assert.That(styles.trouserRoles.Length, Is.EqualTo(12));
            Assert.That(styles.torso.Length, Is.GreaterThan(8));
            var colour = typeof(ClinicWorld).Assembly.GetType("IdleClinic.Presentation.ClinicArt").GetMethod("Color", BindingFlags.Static | BindingFlags.NonPublic);
            var fallback = (Color)colour.Invoke(null, new object[] { "not a clinic role" });
            var roles = styles.hairRoles.Concat(styles.trouserRoles)
                .Concat(styles.headwear.Concat(styles.bodywear).SelectMany(g => g.shapes).Concat(styles.relief.shapes).Select(s => s.role))
                .Where(role => !role.StartsWith("@")).Distinct();
            foreach (var role in roles) Assert.That((Color)colour.Invoke(null, new object[] { role }), Is.Not.EqualTo(fallback), role);
            foreach (var group in styles.headwear.Concat(styles.bodywear))
                foreach (var appearance in group.appearances) Assert.That(appearance, Is.InRange(0, 11));
        }

        [TestCase(0, "Clinic Hair", "Clinic Denim")]
        [TestCase(4, "Clinic HairGold", "Clinic Slate")]
        [TestCase(8, "Clinic HairSilver", "Clinic Slate")]
        public void VisitorsWearTheirOwnHairAndTrouserColours(int appearance, string hair, string trousers)
        {
            var state = new ClinicState();
            state.Patients.Add(new ClinicPatientState { Id = 1, AppearanceId = appearance, Phase = ClinicPatientPhase.ReceptionQueue, FromAnchor = "reception.queue.0", ToAnchor = "reception.queue.0" });
            world.Render(state, 0, true);
            var names = Find("Patient 1").GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterials.Select(m => m.name).ToArray();
            Assert.That(names, Does.Contain(hair));
            Assert.That(names, Does.Contain(trousers));
        }

        [Test]
        public void BeardAndCapSitOnTheHeadRatherThanInsideOrAboveIt()
        {
            var state = new ClinicState();
            state.Patients.Add(new ClinicPatientState { Id = 1, AppearanceId = 8, Phase = ClinicPatientPhase.ReceptionQueue, FromAnchor = "reception.queue.0", ToAnchor = "reception.queue.0" });
            world.Render(state, 0, true);
            var actor = Find("Patient 1");
            var head = actor.GetComponentInChildren<SkinnedMeshRenderer>().bones.First(b => b.name == "head");
            var beard = Find("Silver beard");
            Assert.That(beard.gameObject.activeInHierarchy, Is.True);
            Assert.That(beard.position.y, Is.LessThan(head.position.y), "The beard is on the jaw, below the head bone.");
            Assert.That(Vector3.Dot(beard.position - head.position, actor.forward), Is.InRange(.03f, .10f), "In front of the neck, not floating ahead of the face.");
        }

        private Transform Find(string name) => host.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    }
}
