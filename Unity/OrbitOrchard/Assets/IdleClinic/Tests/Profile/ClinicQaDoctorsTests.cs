#if UNITY_EDITOR
using System;
using System.IO;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The development-only doctors clinic switch builds a save a player could have reached: the starter clinic is
    /// completed through its real purchase rules, the doctors clinic opens through the same path as the entrance button, and
    /// the result survives a relaunch. Previously the switch wrote a doctors clinic that validation rejected.</summary>
    public sealed class ClinicQaDoctorsTests
    {
        private string directory;
        private DateTimeOffset now;

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "clinic-qa-doctors-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            now = new DateTimeOffset(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void TheQaDoctorsClinicIsReachedThroughTheRealRulesAndSurvivesARelaunch()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            ClinicGuideSkipTests.FinishOpening(new ClinicSimulation(store.Profile.state));
            var gems = store.Profile.premium.gems;

            Assert.That(store.QaOpenDoctorsClinic(now), Is.True, store.Error);

            var reloaded = new ClinicProfileStore(directory); reloaded.LoadClinic(now);
            var profile = reloaded.Profile;
            Assert.That(profile.activeLocation, Is.EqualTo(ClinicLocation.DoctorsClinic));
            Assert.That(profile.doctorsState, Is.Not.Null);
            Assert.That(profile.state.DoctorsClinicUnlocked, Is.True);
            Assert.That(ClinicRules.StarterCompletion(profile.state), Is.Empty);
            Assert.That(ClinicSimulation.IsValidState(profile.state) && ClinicSimulation.IsValidState(profile.doctorsState), Is.True);
            Assert.That(profile.state.Wallet, Is.Zero, "The shared wallet moved to the doctors clinic.");
            Assert.That(profile.premium.gems, Is.EqualTo(gems), "QA coins only: no gems are granted or spent.");
            Assert.That(reloaded.SelectLocation(ClinicLocation.StarterClinic, now), Is.True, reloaded.Error);
            Assert.That(reloaded.Profile.doctorsState.Wallet, Is.Zero);
        }

        [Test] public void ADecorQuoteFromTheDoctorsClinicNeverSpendsInTheStarterClinicAfterTravel()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            ClinicGuideSkipTests.FinishOpening(new ClinicSimulation(store.Profile.state));
            Assert.That(store.QaOpenDoctorsClinic(now), Is.True, store.Error);
            Assert.That(store.QaGrantGems(500, now), Is.True, store.Error);
            var quote = store.QuoteGemSpend(ClinicGemSpendKind.Decoration, "Reception", now);
            Assert.That(quote.Available, Is.True, quote.Unavailable);
            Assert.That(quote.Location, Is.EqualTo(ClinicLocation.DoctorsClinic));

            Assert.That(store.SelectLocation(ClinicLocation.StarterClinic, now), Is.True, store.Error);
            var gems = store.Profile.premium.gems;
            var starterDecor = store.Profile.state.Room(ClinicRoom.Reception).DecorationLevel;
            var doctorsDecor = store.Profile.doctorsState.Room(ClinicRoom.Reception).DecorationLevel;
            Assert.That(store.QuoteGemSpend(ClinicGemSpendKind.Decoration, "Reception", now).Location, Is.EqualTo(ClinicLocation.StarterClinic));

            Assert.That(store.SpendGemsFor(ClinicGemSpendKind.Decoration, "Reception", quote.Gems, now, quote.Location), Is.False,
                "The same room name exists in both clinics; the confirmation belongs to the one it was made in.");
            Assert.That(store.Profile.premium.gems, Is.EqualTo(gems));
            Assert.That(store.Profile.state.Room(ClinicRoom.Reception).DecorationLevel, Is.EqualTo(starterDecor));
            Assert.That(store.Profile.doctorsState.Room(ClinicRoom.Reception).DecorationLevel, Is.EqualTo(doctorsDecor));
        }

        [Test] public void TheQaSwitchRefusesASaveStillInTheOpeningAndChangesNothing()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var before = store.Profile.state.Tick;
            Assert.That(store.QaOpenDoctorsClinic(now), Is.False);
            Assert.That(store.Profile.doctorsState, Is.Null);
            Assert.That(store.Profile.state.Tick, Is.EqualTo(before));
        }

        [Test] public void ThePlayerPathStillNeedsACompletedStarterClinic()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            ClinicGuideSkipTests.FinishOpening(new ClinicSimulation(store.Profile.state));
            Assert.That(store.OpenDoctorsClinic(now), Is.False);
            Assert.That(store.Profile.doctorsState, Is.Null);
        }
    }
}
#endif
