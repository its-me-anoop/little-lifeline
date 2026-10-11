using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The guided path: optional gem lessons never block the free lessons after them.</summary>
    public sealed class ClinicGuideTests
    {
        private static readonly string[] ShippedIds =
        {
            "guide.equipment", "guide.waiting", "guide.decor", "guide.renovate", "guide.nurse", "guide.parking",
            "guide.receptionist", "guide.train", "guide.vending", "guide.goals", "guide.finish", "guide.doctors",
            "guide.doctors.consult", "guide.doctors.pharmacy", "guide.doctors.taxi", "guide.doctors.doctor",
            "guide.doctors.tier3", "guide.doctors.parking", "guide.doctors.train", "guide.doctors.tier4",
            "guide.doctors.team", "guide.doctors.tier6"
        };

        [Test] public void ShippedIdsKeepTheirOrderAndRewards()
        {
            Assert.That(ClinicGuide.Steps.Select(s => s.Id), Is.EqualTo(ShippedIds), "Ids are stored with claims: never rename, reorder or drop one.");
            Assert.That(ClinicGuide.Find("guide.decor").GemReward, Is.EqualTo(5));
            Assert.That(ClinicGuide.Find("guide.finish").GemReward, Is.EqualTo(5));
        }

        [Test] public void OnlyTheTwoGemLessonsAreOptional()
        {
            Assert.That(ClinicGuide.Steps.Where(s => s.Optional).Select(s => s.Id), Is.EqualTo(new[] { "guide.decor", "guide.finish" }));
            Assert.That(ClinicGuide.Steps.Where(s => s.Optional).All(s => !string.IsNullOrEmpty(s.FreeAlternative)), Is.True,
                "Every optional lesson says how to carry on without spending.");
        }

        [Test] public void SkippedOptionalLessonsStepAsideAndFreeLessonsCannotBeSkipped()
        {
            var claimed = new[] { "guide.equipment", "guide.waiting" };
            Assert.That(ClinicGuide.Current(claimed, new string[0]).Id, Is.EqualTo("guide.decor"));
            Assert.That(ClinicGuide.Current(claimed, new[] { "guide.decor" }).Id, Is.EqualTo("guide.renovate"),
                "Skipping decor moves straight on to the next free lesson.");
            Assert.That(ClinicGuide.Current(claimed.Concat(new[] { "guide.decor" }).ToArray(), new[] { "guide.renovate" }).Id, Is.EqualTo("guide.renovate"),
                "A free lesson stays current even if a stale skip names it.");

            var beforeFinish = ShippedIds.TakeWhile(id => id != "guide.finish").ToArray();
            Assert.That(ClinicGuide.Current(beforeFinish, new string[0]).Id, Is.EqualTo("guide.finish"));
            Assert.That(ClinicGuide.Current(beforeFinish, new[] { "guide.finish" }).Id, Is.EqualTo("guide.doctors"));
            Assert.That(ClinicGuide.Current(ShippedIds, new string[0]), Is.Null, "Everything collected: the guide is complete.");
            Assert.That(ClinicGuide.Current(ShippedIds.Where(id => id != "guide.decor").ToArray(), new[] { "guide.decor" }), Is.Null,
                "Only a skipped optional lesson left: the guide is complete.");
            Assert.That(ClinicGuide.CanSkip(ClinicGuide.Find("guide.decor")), Is.True);
            Assert.That(ClinicGuide.CanSkip(ClinicGuide.Find("guide.renovate")), Is.False);
            Assert.That(ClinicGuide.CanSkip(null), Is.False);
        }

        [Test] public void FinishingEarlyIsTheFinishLessonNotAnyEarlierGemSpending()
        {
            var starter = ClinicSimulation.CreateNew().State;
            var finish = ClinicGuide.Find("guide.finish");
            Assert.That(finish.IsDone(starter, null, new ClinicGuideProgress(0, 200)), Is.False, "Decor or shop spending is not finishing work early.");
            Assert.That(finish.IsDone(starter, null, new ClinicGuideProgress(0, 0, 1, false)), Is.True, "A free last-minute finish counts.");
            Assert.That(finish.IsDone(starter, null, new ClinicGuideProgress(0, 30, 1, false)), Is.True);
            Assert.That(finish.IsDone(starter, null, new ClinicGuideProgress(0, 30, 0, true)), Is.True,
                "A save from before this fix that had already spent gems keeps its finished lesson.");
        }
    }
}
