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
            Assert.That(ends - sim.State.Tick, Is.EqualTo(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.CleanOffice)));
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
            Assert.That(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.CleanOffice), Is.GreaterThan(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.CleanReception)));
            Assert.That(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.SetUpOffice), Is.GreaterThan(ClinicSimulation.OpeningTaskTicks(ClinicOpeningStep.SetUpNursing)));
            foreach (ClinicOpeningStep step in Enum.GetValues(typeof(ClinicOpeningStep)))
                if (ClinicSimulation.OpeningManual(step)) Assert.That(ClinicSimulation.OpeningTaskTicks(step), Is.InRange(30, 100), step.ToString());
        }

        [Test] public void TheOpeningReachesItsFirstPatientRewardAndChoiceQuickly()
        {
            var passive = ClinicOpeningPacing.Measure();
            TestContext.WriteLine("Opening, player waits: " + passive);
            var helping = ClinicOpeningPacing.Measure(helpEvery: .6);
            TestContext.WriteLine("Opening, player helps the boss: " + helping);
            // 4.3 (22), measured the same way: first patient 42s, first coins 62s, clinic open 115s, 100s of passive waiting.
            Assert.That(passive.FirstPatientSeconds, Is.LessThanOrEqualTo(32), "The first patient arrives within about half a minute.");
            Assert.That(passive.FirstCoinsSeconds, Is.LessThanOrEqualTo(52), "The first coins are in hand in under a minute.");
            Assert.That(passive.OpenSeconds, Is.LessThanOrEqualTo(90), "The whole opening, including recruiting, takes about a minute and a half.");
            Assert.That(passive.PassiveSeconds, Is.LessThanOrEqualTo(72), "At least a quarter less time watching timers.");
            Assert.That(passive.FirstDecisionSeconds, Is.LessThanOrEqualTo(passive.OpenSeconds + 60), "A real choice follows the opening within a minute.");
            Assert.That(helping.OpenSeconds, Is.LessThan(passive.OpenSeconds), "Helping the boss is faster than waiting.");
            Assert.That(helping.FirstPatientSeconds, Is.GreaterThan(12), "Helping speeds the boss up; it does not skip the opening.");
        }
    }
}
