using System;
using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>Plays the boss opening the way a player does: each prompt is answered after a reaction time, the first payment is
    /// collected, and nothing else is tapped. Measures what a new player waits for.</summary>
    public sealed class ClinicOpeningPacing
    {
        public double FirstPatientSeconds = -1, FirstPaymentSeconds = -1, FirstCoinsSeconds = -1, OpenSeconds = -1, FirstDecisionSeconds = -1;
        /// <summary>Seconds with nothing to tap while the boss works or the first visitors arrive.</summary>
        public double PassiveSeconds;
        public int Taps, HelpTaps;

        /// <param name="reaction">Seconds a player takes to answer a new prompt.</param>
        /// <param name="helpEvery">If positive, the player also taps Help every this many seconds while the boss works.</param>
        public static ClinicOpeningPacing Measure(double reaction = 1.5, double helpEvery = 0)
        {
            var result = new ClinicOpeningPacing();
            var sim = ClinicSimulation.CreateOpening();
            ClinicSimulation.TryGrantReward(sim.State, 250); // what a new profile starts with
            double Now() => sim.State.Tick / (double)ClinicRules.TicksPerSecond;
            const double step = .1;
            for (var guard = 0; guard < 200000 && sim.State.OpeningStep != ClinicOpeningStep.Complete; guard++)
            {
                var opening = sim.State.OpeningStep;
                var busy = sim.State.OpeningTaskEndsTick >= 0;
                var till = sim.State.ReceptionDesks.Any(d => d.Till > 0);
                if (!busy && (ClinicSimulation.OpeningManual(opening) || opening == ClinicOpeningStep.HireNurse || opening == ClinicOpeningStep.HireReceptionist))
                {
                    sim.Advance(reaction, false);
                    Assert.That(sim.ContinueOpening(opening).Success, Is.True, opening.ToString());
                    result.Taps++;
                }
                else if (till)
                {
                    if (result.FirstPaymentSeconds < 0) result.FirstPaymentSeconds = Now();
                    sim.Advance(reaction, false);
                    foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
                    if (result.FirstCoinsSeconds < 0) result.FirstCoinsSeconds = Now();
                    result.Taps++;
                }
                else if (busy && helpEvery > 0)
                {
                    sim.Advance(helpEvery, false);
                    result.PassiveSeconds += helpEvery;
                    if (sim.State.OpeningTaskEndsTick >= 0 && sim.HelpOpeningTask().Success) { result.HelpTaps++; result.Taps++; }
                }
                else { sim.Advance(step, false); result.PassiveSeconds += step; }
                if (result.FirstPatientSeconds < 0 && sim.State.Patients.Count > 0) result.FirstPatientSeconds = Now();
                if (result.FirstPaymentSeconds < 0 && sim.State.TotalPayments > 0) result.FirstPaymentSeconds = Now();
                // In the opening the boss banks the first payment, so it reaches the wallet without a tap.
                if (result.FirstCoinsSeconds < 0 && sim.State.Wallet > 250) result.FirstCoinsSeconds = Now();
            }
            result.OpenSeconds = Now();
            // The first real choice: two or more different improvements the wallet can already pay for.
            for (var guard = 0; guard < 20000 && result.FirstDecisionSeconds < 0; guard++)
            {
                foreach (var desk in sim.State.ReceptionDesks) if (desk.Till > 0) sim.Collect(desk.Id);
                if (ClinicPurchases.Options(sim).Count(o => o.Cost <= sim.State.Wallet) >= 2) result.FirstDecisionSeconds = Now();
                else sim.Advance(1, false);
            }
            Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True);
            return result;
        }

        public override string ToString() =>
            $"first patient {FirstPatientSeconds:0.0}s, first payment {FirstPaymentSeconds:0.0}s, coins in hand {FirstCoinsSeconds:0.0}s, " +
            $"clinic open {OpenSeconds:0.0}s, first choice of 2+ affordable improvements {FirstDecisionSeconds:0.0}s, " +
            $"passive waiting {PassiveSeconds:0.0}s, taps {Taps} (help {HelpTaps})";
    }

    public sealed class ClinicOpeningPacingTests
    {
        [Test] public void ASaveMadeMidTaskWithTheLongerTimersStaysValidAndFinishes()
        {
            foreach (var (step, ticks) in new[] { (ClinicOpeningStep.CleanOffice, 80), (ClinicOpeningStep.SetUpOffice, 100) })
            {
                var sim = ClinicSimulation.CreateOpening();
                while (sim.State.OpeningStep != step) { sim.ContinueOpening(sim.State.OpeningStep); sim.Advance(12, false); }
                Assert.That(sim.ContinueOpening(step).Success, Is.True);
                // As 4.3 (22) saved it: the same task with its longer timer, part done.
                sim.State.OpeningTaskEndsTick = sim.State.OpeningTaskStartedTick + ticks;
                sim.Advance(3, false);
                Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True, step + " with the " + ticks / 10 + "s timer");
                var resumed = new ClinicSimulation(sim.State);
                resumed.Advance(ticks / 10d, false);
                Assert.That(resumed.State.OpeningStep, Is.EqualTo(step + 1));
                Assert.That(ClinicSimulation.IsValidState(resumed.State), Is.True);
            }
        }

        [Test] public void HelpingShortensOnlyTheCurrentTaskAndNeverSkipsIt()
        {
            var sim = ClinicSimulation.CreateOpening();
            Assert.That(sim.HelpOpeningTask().Success, Is.False, "Nothing to help with before a task starts.");
            Assert.That(sim.ContinueOpening(ClinicOpeningStep.CleanOffice).Success, Is.True);
            var ends = sim.State.OpeningTaskEndsTick;
            Assert.That(ends - sim.State.Tick, Is.EqualTo(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.CleanOffice, 3)), "The boss starts in the office: no walk.");
            Assert.That(sim.HelpOpeningTask().Success, Is.True);
            Assert.That(sim.State.OpeningTaskEndsTick, Is.EqualTo(ends - ClinicSimulation.OpeningHelpTicks));
            for (var tap = 0; tap < 20; tap++) sim.HelpOpeningTask();
            Assert.That(sim.State.OpeningTaskEndsTick, Is.EqualTo(sim.State.Tick + 1), "However fast the taps, the task still runs.");
            Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.CleanOffice));
            Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True);
            sim.Advance(.1, false);
            Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.SetUpOffice), "Then the opening carries on in order.");
            Assert.That(sim.State.OfficeCleaned, Is.True);
            Assert.That(sim.HelpOpeningTask().Success, Is.False);
        }

        [Test] public void LaterRoomsRepeatTheRhythmMoreBrieflyThanTheOffice()
        {
            Assert.That(ClinicSimulation.OpeningWorkTicks(ClinicOpeningStep.CleanOffice), Is.GreaterThan(ClinicSimulation.OpeningWorkTicks(ClinicOpeningStep.CleanReception)));
            Assert.That(ClinicSimulation.OpeningWorkTicks(ClinicOpeningStep.SetUpOffice), Is.GreaterThan(ClinicSimulation.OpeningWorkTicks(ClinicOpeningStep.SetUpNursing)));
            // Each task as a player meets it, walking from where the previous one left the boss.
            var zone = 3;
            foreach (ClinicOpeningStep step in Enum.GetValues(typeof(ClinicOpeningStep)))
            {
                if (!ClinicSimulation.OpeningManual(step)) continue;
                Assert.That(ClinicSimulation.OpeningTaskTicks(step, zone), Is.InRange(30, 100), step.ToString());
                zone = ClinicSimulation.OpeningZone(step);
            }
        }

        [Test] public void TheBossWalksEveryRouteAtOnePaceAndTheWalkFitsInsideTheTask()
        {
            for (var from = 0; from <= 3; from++)
                for (var to = 0; to <= 3; to++)
                {
                    var ticks = ClinicBossRoute.TravelTicks(from, to);
                    if (from == to) { Assert.That(ticks, Is.Zero); continue; }
                    var pace = ClinicBossRoute.Length(from, to) / (ticks / (double)ClinicRules.TicksPerSecond);
                    Assert.That(pace, Is.InRange(ClinicBossRoute.MetresPerSecond * .9, ClinicBossRoute.MetresPerSecond), from + " to " + to);
                }
            // The routes past the nursing doorway: the waiting room to nursing crosses it.
            Assert.That(ClinicBossRoute.Points(2, 1).Any(p => Math.Abs(p.X + 4.20) < .01 && Math.Abs(p.Z - 1.50) < .01), Is.True);

            var sim = ClinicSimulation.CreateOpening();
            foreach (ClinicOpeningStep step in Enum.GetValues(typeof(ClinicOpeningStep)))
            {
                if (!ClinicSimulation.OpeningManual(step)) continue;
                for (var guard = 0; guard < 2000 && sim.State.OpeningStep != step; guard++) sim.Advance(.1, false);
                Assert.That(sim.ContinueOpening(step).Success, Is.True, step.ToString());
                var walk = ClinicSimulation.OpeningWalkTicks(sim.State);
                Assert.That(sim.State.OpeningTaskEndsTick - sim.State.OpeningTaskStartedTick, Is.GreaterThan(walk), step + ": the boss arrives, then works.");
                Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True, step.ToString());
            }
        }

        [Test] public void HelpingNeverShortensTheWalkSoTheBossCannotJumpAhead()
        {
            var sim = ClinicSimulation.CreateOpening();
            foreach (var step in new[] { ClinicOpeningStep.CleanOffice, ClinicOpeningStep.SetUpOffice })
            { sim.ContinueOpening(step); for (var guard = 0; guard < 200 && sim.State.OpeningStep == step; guard++) sim.Advance(.1, false); }
            Assert.That(sim.ContinueOpening(ClinicOpeningStep.CleanReception).Success, Is.True);
            var walk = ClinicSimulation.OpeningWalkTicks(sim.State);
            Assert.That(walk, Is.EqualTo(ClinicBossRoute.TravelTicks(3, 0)).And.GreaterThan(0), "The office to reception is a real walk.");
            var arrives = sim.State.OpeningTaskStartedTick + walk;
            for (var tap = 0; tap < 30; tap++) sim.HelpOpeningTask();
            Assert.That(sim.State.OpeningTaskEndsTick, Is.EqualTo(arrives + 1), "Help takes time off the cleaning, never off the walk.");
            Assert.That(sim.HelpOpeningTask().Success, Is.False);
            sim.Advance((walk - 1) / 10d, false);
            Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.CleanReception), "Still on the way.");
            Assert.That(ClinicSimulation.IsValidState(sim.State), Is.True);
            sim.Advance(.3, false);
            Assert.That(sim.State.OpeningStep, Is.EqualTo(ClinicOpeningStep.SetUpReception));
        }

        [Test] public void TheOpeningReachesItsFirstPatientRewardAndChoiceQuickly()
        {
            var passive = ClinicOpeningPacing.Measure();
            TestContext.WriteLine("Opening, player waits: " + passive);
            var helping = ClinicOpeningPacing.Measure(helpEvery: .6);
            TestContext.WriteLine("Opening, player helps the boss: " + helping);
            // 4.3 (22), measured the same way: first patient 42s, first coins 62s, clinic open 115s, 100s of passive waiting.
            // The boss walks between rooms at one steady pace (see ClinicBossRoute), which costs a few seconds over fixed short timers
            // but keeps the walk believable and the doors opening.
            Assert.That(passive.FirstPatientSeconds, Is.LessThanOrEqualTo(36), "The first patient arrives sooner than 4.3 (22)'s 42s.");
            Assert.That(passive.FirstCoinsSeconds, Is.LessThanOrEqualTo(56), "The first coins are in hand in under a minute.");
            Assert.That(passive.OpenSeconds, Is.LessThanOrEqualTo(96), "The whole opening, including recruiting, takes about a minute and a half.");
            Assert.That(passive.PassiveSeconds, Is.LessThanOrEqualTo(80), "At least a fifth less time watching timers.");
            Assert.That(passive.FirstDecisionSeconds, Is.LessThanOrEqualTo(passive.OpenSeconds + 60), "A real choice follows the opening within a minute.");
            Assert.That(helping.OpenSeconds, Is.LessThan(passive.OpenSeconds), "Helping the boss is faster than waiting.");
            Assert.That(helping.FirstPatientSeconds, Is.GreaterThan(12), "Helping speeds the boss up; it does not skip the opening.");
        }
    }
}
