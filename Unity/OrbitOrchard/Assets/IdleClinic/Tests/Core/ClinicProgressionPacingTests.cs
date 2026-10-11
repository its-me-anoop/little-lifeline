using System;
using System.Collections.Generic;
using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>Free progression, measured. A free player opens the clinic, collects every till and buys the cheapest open coin
    /// improvement whenever it can pay; no gems, no second builder, no coin rewards from goals. Two habits are measured:
    /// continuous play, and three ten-minute sessions a day with the clinic earning offline (eight-hour limit) in between.</summary>
    public sealed class ClinicFreeProgression
    {
        public sealed class Milestone { public string Name; public double PlayHours = -1, Day = -1; }
        public readonly List<Milestone> Milestones = new List<Milestone>();
        /// <summary>Longest active stretch with nothing affordable to buy, by stage (play-time hours).</summary>
        public double LongestGapEarly, LongestGapMid, LongestGapLate;
        public readonly List<(int day, int purchases, double gemsFree)> SessionLog = new List<(int, int, double)>();
        public int Purchases;
        public double ActiveHours, Days;
        public string Bottleneck = "";

        private static readonly (string name, Func<ClinicState, bool> met)[] Goals =
        {
            ("first renovation", s => s.Rooms.Any(r => r.Tier >= 2)),
            ("clinic level 3", s => s.OfficeLevel >= 3),
            ("clinic level 5", s => s.OfficeLevel >= 5),
            ("clinic level 10", s => s.OfficeLevel >= 10),
            ("clinic level 15", s => s.OfficeLevel >= 15),
            ("clinic level 20", s => s.OfficeLevel >= 20),
            ("starter complete", s => ClinicRules.StarterCompletion(s).Count == 0),
        };

        public static ClinicSimulation OpenedClinic()
        {
            var sim = ClinicSimulation.CreateOpening();
            ClinicSimulation.TryGrantReward(sim.State, 250);
            for (var guard = 0; guard < 400 && sim.State.OpeningStep != ClinicOpeningStep.Complete; guard++)
            {
                var step = sim.State.OpeningStep;
                if (sim.State.OpeningTaskEndsTick < 0 && step != ClinicOpeningStep.FirstCheckIn && step != ClinicOpeningStep.QueueArrivals) sim.ContinueOpening(step);
                sim.Advance(1, false);
            }
            return sim;
        }

        private static void CollectAll(ClinicSimulation sim)
        {
            foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
            sim.CollectVendingTips(); sim.CollectParkingFees();
        }

        private static int BuyAffordable(ClinicSimulation sim)
        {
            var bought = 0;
            for (var i = 0; i < 200; i++)
            {
                var cheapest = ClinicPurchases.Options(sim).OrderBy(o => o.Cost).FirstOrDefault();
                if (cheapest == null || cheapest.Cost > sim.State.Wallet || !ClinicPurchases.Buy(sim, cheapest).Success) break;
                bought++;
            }
            return bought;
        }

        private void Mark(ClinicState state, double hours, double day)
        {
            foreach (var (name, met) in Goals)
                if (Milestones.All(m => m.Name != name) && met(state)) Milestones.Add(new Milestone { Name = name, PlayHours = hours, Day = day });
        }

        /// <summary>Continuous play until the starter clinic is complete or <paramref name="maxHours"/> pass.</summary>
        public static ClinicFreeProgression Continuous(double maxHours = 200)
        {
            var r = new ClinicFreeProgression(); var sim = OpenedClinic();
            double Hours() => sim.State.Tick / 36000d;
            var lastPurchase = Hours();
            while (Hours() < maxHours && ClinicRules.StarterCompletion(sim.State).Count > 0)
            {
                sim.Advance(30, false); CollectAll(sim);
                var bought = BuyAffordable(sim);
                r.Purchases += bought;
                if (bought > 0)
                {
                    var gap = Hours() - lastPurchase;
                    if (lastPurchase < 1) r.LongestGapEarly = Math.Max(r.LongestGapEarly, gap);
                    else if (lastPurchase < 10) r.LongestGapMid = Math.Max(r.LongestGapMid, gap);
                    else r.LongestGapLate = Math.Max(r.LongestGapLate, gap);
                    lastPurchase = Hours();
                }
                r.Mark(sim.State, Hours(), -1);
            }
            r.ActiveHours = Hours();
            var next = ClinicPurchases.Suggest(sim);
            r.Bottleneck = next == null ? "nothing left" : ClinicPurchases.Describe(sim.State, next) + " " + next.Cost + " coins vs " + Math.Round(EarningPerHour(sim)) + " coins/hour";
            Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True);
            return r;
        }

        /// <summary>Sessions of <paramref name="sessionMinutes"/> at the given hours each day, the clinic earning offline between them.</summary>
        public static ClinicFreeProgression Habit(int days, double sessionMinutes = 10, double[] sessionHours = null)
        {
            sessionHours = sessionHours ?? new[] { 8d, 13d, 20d };
            var r = new ClinicFreeProgression(); var sim = OpenedClinic();
            var clock = sessionHours[0] * 3600d; // seconds since day 0 midnight; the opening was played in the first session
            for (var day = 0; day < days && ClinicRules.StarterCompletion(sim.State).Count > 0; day++)
                foreach (var hour in sessionHours)
                {
                    var start = day * 86400d + hour * 3600d;
                    if (start > clock)
                    {
                        sim.AdvanceOffline(start - clock, ClinicRules.MaximumOfflineSeconds, ClinicRules.OfflineCoinCap(sim.State));
                        clock = start;
                    }
                    var bought = 0;
                    for (var t = 0d; t < sessionMinutes * 60; t += 30)
                    {
                        CollectAll(sim); bought += BuyAffordable(sim);
                        sim.Advance(30, false); clock += 30; r.ActiveHours += 30 / 3600d;
                    }
                    CollectAll(sim); bought += BuyAffordable(sim);
                    r.Purchases += bought;
                    // Free gems a player can also collect each day (login streak average and three daily goals with the bonus).
                    r.SessionLog.Add((day + 1, bought, 0));
                    r.Mark(sim.State, r.ActiveHours, day + 1);
                    r.Days = day + 1;
                }
            var next = ClinicPurchases.Suggest(sim);
            r.Bottleneck = next == null ? "nothing left" : ClinicPurchases.Describe(sim.State, next) + " " + next.Cost + " coins vs " + Math.Round(EarningPerHour(sim)) + " coins/hour";
            Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True);
            return r;
        }

        private static double EarningPerHour(ClinicSimulation sim)
        {
            var copy = new ClinicSimulation(ClinicDoctorsTests.Clone(sim.State));
            var earned = copy.State.TotalEarned; copy.Advance(600, false);
            return (copy.State.TotalEarned - earned) * 6d;
        }

        public string Describe(bool days)
            => string.Join(", ", Milestones.Select(m => m.Name + " " + (days ? "day " + m.Day : m.PlayHours.ToString("0.0") + "h")));
    }

    public sealed class ClinicProgressionPacingTests
    {
        [Test] public void FreeProgressionIsMeasuredForContinuousAndSessionPlay()
        {
            var continuous = ClinicFreeProgression.Continuous();
            TestContext.WriteLine("Continuous free play: " + continuous.Describe(false) + $". {continuous.Purchases} purchases in {continuous.ActiveHours:0.0}h.");
            TestContext.WriteLine($"Longest wait for the next affordable improvement: first hour {continuous.LongestGapEarly * 60:0.0} min, hours 1-10 {continuous.LongestGapMid * 60:0.0} min, after 10h {continuous.LongestGapLate * 60:0.0} min.");
            TestContext.WriteLine("At the end: next " + continuous.Bottleneck);

            var casual = ClinicFreeProgression.Habit(60);
            TestContext.WriteLine("Three 10-minute sessions a day: " + casual.Describe(true) + $". {casual.Purchases} purchases over {casual.Days} days ({casual.ActiveHours:0.0}h active).");
            foreach (var day in new[] { 1, 2, 3, 7, 14, 30, 60 })
            {
                var sessions = casual.SessionLog.Where(s => s.day == day).ToList();
                if (sessions.Count > 0) TestContext.WriteLine($"  day {day}: purchases per session {string.Join("/", sessions.Select(s => s.purchases))}");
            }
            TestContext.WriteLine("After 60 days: next " + casual.Bottleneck);

            // Sanity: every free player keeps progressing; nothing here asserts a target grind length.
            Assert.That(continuous.Milestones.Select(m => m.Name), Does.Contain("first renovation").And.Contain("clinic level 5"));
            Assert.That(casual.SessionLog.Take(9).All(s => s.purchases > 0), Is.True, "Every session of the first three days buys something.");
            Assert.That(continuous.LongestGapEarly * 60, Is.LessThan(5), "In the first hour there is always something to buy within minutes.");
        }
    }
}
