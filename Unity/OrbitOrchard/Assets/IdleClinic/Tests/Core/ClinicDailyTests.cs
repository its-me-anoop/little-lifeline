using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>Daily goals are chosen once a day, can always be met, and match the previous goals where they could.</summary>
    public sealed class ClinicDailyTests
    {
        [Test] public void WhereEveryGoalCanBeMetThePlanMatchesThePreviousGoals()
        {
            var clinic = ClinicSimulation.CreateNew().State;
            for (var day = 280; day < 340; day++)
                foreach (var parking in new[] { false, true })
                {
                    var before = ClinicDaily.Goals(day, clinic, parking);
                    var plan = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, HasParking = parking, HasVending = true, SpendableCoins = long.MaxValue });
                    Assert.That(plan.Select(g => g.Id + ":" + g.Target + ":" + g.CoinReward + ":" + g.GemReward),
                        Is.EqualTo(before.Select(g => g.Id + ":" + g.Target + ":" + g.CoinReward + ":" + g.GemReward)), "Day " + day);
                }
        }

        [Test] public void AFullyUpgradedClinicNeverGetsASpendingGoalItCannotMeet()
        {
            var maxed = DoctorsProgressionFixture.MaxStarter();
            Assert.That(ClinicPurchases.Options(maxed, true), Is.Empty, "Nothing is left to buy with coins.");
            Assert.That(ClinicPurchases.NextCostsTotal(maxed), Is.Zero);
            for (var day = 280; day < 400; day++)
                foreach (var parking in new[] { false, true })
                {
                    var plan = ClinicDaily.Plan(day, new ClinicDailyContext { Active = maxed.State, HasParking = parking, HasVending = true,
                        SpendableCoins = ClinicPurchases.NextCostsTotal(maxed) });
                    Assert.That(plan.Count, Is.EqualTo(ClinicDaily.GoalsPerDay), "Day " + day);
                    Assert.That(plan.Select(g => g.Id), Is.Unique);
                    Assert.That(plan.Any(g => g.Measure == ClinicDailyMeasure.Spent), Is.False, "Day " + day);
                    Assert.That(plan.All(g => g.Target > 0 && g.CoinReward > 0 && g.GemReward > 0), Is.True);
                }
        }

        [Test] public void AClinicWithLittleLeftToBuyOnlyGetsSpendingGoalsItCanMeet()
        {
            var clinic = ClinicSimulation.CreateNew().State;
            for (var day = 280; day < 340; day++)
            {
                var spend = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, SpendableCoins = long.MaxValue }).First(g => g.Id == "daily.spend");
                var plan = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, SpendableCoins = spend.Target - 1 });
                Assert.That(plan.Select(g => g.Id), Does.Not.Contain("daily.spend"));
                Assert.That(plan.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
                var enough = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, SpendableCoins = spend.Target });
                Assert.That(enough.Select(g => g.Id), Does.Contain("daily.spend"));
            }
        }

        [Test] public void CollectedGoalsAreAlwaysKeptAndNeverDropped()
        {
            var clinic = ClinicSimulation.CreateNew().State;
            for (var day = 280; day < 340; day++)
                foreach (var kept in new[] { "daily.spend", "daily.parking", "daily.treat", "daily.collect" })
                {
                    var plan = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, HasParking = true, SpendableCoins = 0, Keep = new[] { kept } });
                    Assert.That(plan.Select(g => g.Id), Does.Contain(kept), "Day " + day + " keeps " + kept);
                    Assert.That(plan.Count, Is.EqualTo(ClinicDaily.GoalsPerDay));
                }
        }

        [Test] public void NewKindsOfGoalCountFromWhenTheyAreSetAndEarlierKindsFromTheDayStart()
        {
            var clinic = ClinicSimulation.CreateNew().State;
            var dayStart = new ClinicDailyTotals(10, 1000, 500, 0, 40, 12);
            var now = new ClinicDailyTotals(25, 1600, 900, 0, 70, 30);
            for (var day = 280; day < 340; day++)
            {
                var plan = ClinicDaily.Plan(day, new ClinicDailyContext { Active = clinic, SpendableCoins = 0, HasVending = false, DayStart = dayStart, Now = now });
                var checkIn = plan.First(g => g.Id == "daily.checkin");
                Assert.That(checkIn.StartValue, Is.EqualTo(30));
                Assert.That(checkIn.Progress(now), Is.Zero);
                var treat = plan.First(g => g.Id == "daily.treat");
                Assert.That(treat.StartValue, Is.EqualTo(10));
                Assert.That(treat.Progress(now), Is.EqualTo(15));
            }
        }

        [Test] public void StoredGoalsRestoreExactlyAndUnknownIdsStayReadable()
        {
            var goal = ClinicDaily.Restore("daily.collect", ClinicDailyMeasure.Collected, 4000, 1000, 3, 77);
            Assert.That(ClinicDaily.Describe(goal), Is.EqualTo("Collect 4,000 coins"));
            Assert.That(goal.Progress(new ClinicDailyTotals(0, 4077, 0, 0)), Is.EqualTo(4000));
            Assert.That(goal.IsDone(new ClinicDailyTotals(0, 4076, 0, 0)), Is.False);
            Assert.That(ClinicDaily.IsKnown("daily.someday"), Is.False);
            Assert.That(ClinicDaily.Describe(ClinicDaily.Restore("daily.someday", ClinicDailyMeasure.Treatments, 5, 1, 1, 0)), Does.Contain("5"));
        }
    }
}
