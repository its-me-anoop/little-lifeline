using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IdleClinic.Core;
using IdleClinic.Services;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    public sealed class DoctorsProfileTests
    {
        [Serializable] private sealed class Envelope { public int schemaVersion = 1; public string payload, checksum; }
        private string directory, path, fixture;
        private DateTimeOffset now;

        [SetUp] public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "doctors-profile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, ClinicProfileStore.FileName);
            // Actual installed 3.2(16) simulator campaign, copied before this migration.
            fixture = File.ReadAllText(Path.Combine(Application.dataPath, "IdleClinic/Tests/Profile/Fixtures/clinic-3.2.json"));
            File.WriteAllText(path, fixture);
            var original = JsonUtility.FromJson<ClinicProfile>(JsonUtility.FromJson<Envelope>(fixture).payload);
            now = new DateTimeOffset(original.lastAccountedUtcTicks, TimeSpan.Zero);
        }
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test] public void LockedLocationRemainsAbsentAcrossUnityJsonRoundTrip()
        {
            var profile = new ClinicProfile { state = ClinicSimulation.CreateNew().State, lastAccountedUtcTicks = now.UtcDateTime.Ticks };
            var clone = Clone(profile);
            Assert.That(clone.additionalClinics, Is.Empty);
            Assert.That(clone.doctorsState, Is.Null);
            Assert.That(ClinicSimulation.IsValidState(clone.state), Is.True);
        }

        [Test] public void ChangingAudioDuringFailedMigrationDoesNotPreventRetry()
        {
            Directory.CreateDirectory(path + ".tmp");
            var store = new ClinicProfileStore(directory);
            var loaded = store.LoadClinic(now);
            Assert.That(store.HasPendingOfflineProgress, Is.True);
            loaded.preferences.music = false;
            loaded.preferences.sound = false;
            Directory.Delete(path + ".tmp");
            store.ApplyOffline(now);
            Assert.That(store.HasPendingOfflineProgress, Is.False, store.Error);
            var reopened = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(reopened.preferences.music, Is.False);
            Assert.That(reopened.preferences.sound, Is.False);
            Assert.That(reopened.state.Wallet, Is.EqualTo(82520));
        }

        [Test] public void ActualVersionTwoMigratesWithoutChangingMoneyOrImportingALocation()
        {
            var original = JsonUtility.FromJson<ClinicProfile>(JsonUtility.FromJson<Envelope>(fixture).payload);
            var store = new ClinicProfileStore(directory);
            var profile = store.LoadClinic(now);
            Assert.That(store.Error, Is.Null.Or.Empty);
            Assert.That(profile.schemaVersion, Is.EqualTo(ClinicProfile.CurrentSchemaVersion));
            Assert.That(profile.state.SchemaVersion, Is.EqualTo(3));
            Assert.That(profile.state.Wallet, Is.EqualTo(original.state.Wallet));
            Assert.That(profile.state.TotalCollected, Is.EqualTo(original.state.TotalCollected));
            Assert.That(profile.state.TotalEarned, Is.EqualTo(original.state.TotalEarned));
            Assert.That(profile.state.TotalSpent, Is.EqualTo(original.state.TotalSpent));
            Assert.That(profile.state.ReceptionDesks.Sum(d => d.Till), Is.EqualTo(original.state.ReceptionDesks.Sum(d => d.Till)));
            Assert.That(profile.state.Patients.Select(p => p.Id), Is.EqualTo(original.state.Patients.Select(p => p.Id)));
            Assert.That(profile.activeLocation, Is.EqualTo(ClinicLocation.StarterClinic));
            Assert.That(profile.doctorsState, Is.Null);
            Assert.That(profile.state.DoctorsClinicUnlocked, Is.False);
            Assert.That(File.ReadAllText(path + ".v2-before-migration-" + original.revision), Is.EqualTo(fixture));
            Assert.That(JsonUtility.ToJson(new ClinicProfileStore(directory).LoadClinic(now)), Is.EqualTo(JsonUtility.ToJson(profile)));
        }

        [TestCase(false)] [TestCase(true)]
        public void OldSoundChoiceAlsoInitializesMusicAndThenControlsPersistIndependently(bool sound)
        {
            var env = JsonUtility.FromJson<Envelope>(fixture);
            env.payload = env.payload.Replace("\"sound\":true", "\"sound\":" + (sound ? "true" : "false"));
            WriteEnvelope(env.payload);
            var store = new ClinicProfileStore(directory); var profile = store.LoadClinic(now);
            Assert.That(profile.preferences.sound, Is.EqualTo(sound));
            Assert.That(profile.preferences.music, Is.EqualTo(sound));
            profile.preferences.music = !sound;
            Assert.That(store.Save(profile, now), Is.True, store.Error);
            var reloaded = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(reloaded.preferences.sound, Is.EqualTo(sound));
            Assert.That(reloaded.preferences.music, Is.EqualTo(!sound));
        }

        [Test] public void UnfinishedStarterCannotChargeOrCreateAnotherLocation()
        {
            var store = new ClinicProfileStore(directory); var before = JsonUtility.ToJson(store.LoadClinic(now));
            Assert.That(store.OpenDoctorsClinic(now), Is.False);
            Assert.That(JsonUtility.ToJson(store.Profile), Is.EqualTo(before));
            Assert.That(store.SelectLocation(ClinicLocation.DoctorsClinic, now), Is.False);
        }

        [Test] public void OpeningChargesOnceAndTravelMovesOnlyCollectedWallet()
        {
            var store = CompletedStarter(); var before = Clone(store.Profile);
            Assert.That(store.OpenDoctorsClinic(now), Is.True, store.Error);
            var opened = store.Profile;
            Assert.That(opened.state.Wallet, Is.Zero);
            Assert.That(opened.ActiveState.Wallet, Is.EqualTo(before.state.Wallet - 100000));
            Assert.That(opened.state.TotalSpent, Is.EqualTo(before.state.TotalSpent + 100000));
            Assert.That(opened.state.Patients.Select(p => p.Id), Is.EqualTo(before.state.Patients.Select(p => p.Id)));
            Assert.That(opened.state.ReceptionDesks.Sum(d => d.Till), Is.EqualTo(before.state.ReceptionDesks.Sum(d => d.Till)));
            Assert.That(opened.doctorsState.Staff.Count, Is.EqualTo(4), "One of every role avoids an empty-wallet dead end.");
            var snapshot = JsonUtility.ToJson(opened);
            Assert.That(store.OpenDoctorsClinic(now), Is.False);
            Assert.That(JsonUtility.ToJson(store.Profile), Is.EqualTo(snapshot));
            Assert.That(store.Save(before, now), Is.False, "A stale pre-opening object cannot overwrite the committed campaign.");
            for (var i = 0; i < 5; i++)
            {
                Assert.That(store.SelectLocation(ClinicLocation.StarterClinic, now), Is.True, store.Error);
                Assert.That(store.Profile.doctorsState.Wallet, Is.Zero);
                Assert.That(store.Profile.state.Wallet, Is.EqualTo(before.state.Wallet - 100000));
                Assert.That(store.SelectLocation(ClinicLocation.DoctorsClinic, now), Is.True, store.Error);
            }
            Assert.That(JsonUtility.ToJson(new ClinicProfileStore(directory).LoadClinic(now)), Is.EqualTo(JsonUtility.ToJson(store.Profile)));
        }

        [Test] public void FailedOpeningAndTravelWritesLeavePublishedProfileAndDiskUntouched()
        {
            var store = CompletedStarter(); var profile = store.Profile;
            var before = JsonUtility.ToJson(profile); var disk = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.OpenDoctorsClinic(now), Is.False);
            Assert.That(store.Profile, Is.SameAs(profile));
            Assert.That(JsonUtility.ToJson(profile), Is.EqualTo(before));
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
            Directory.Delete(path + ".tmp");
            Assert.That(store.OpenDoctorsClinic(now), Is.True, store.Error);
            before = JsonUtility.ToJson(store.Profile); disk = File.ReadAllText(path);
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.SelectLocation(ClinicLocation.StarterClinic, now), Is.False);
            Assert.That(JsonUtility.ToJson(store.Profile), Is.EqualTo(before));
            Assert.That(File.ReadAllText(path), Is.EqualTo(disk));
        }

        [Test] public void BothClinicsAdvanceAgainstOneWatermarkAndCommitOfflineOnlyOnce()
        {
            var store = CompletedStarter(); Assert.That(store.OpenDoctorsClinic(now), Is.True, store.Error);
            var expected = Clone(store.Profile);
            var first = new ClinicSimulation(expected.state).AdvanceOffline(36 * 3600, ClinicRules.MaximumOfflineSeconds, ClinicRules.OfflineCoinCap(expected.state, 1));
            var second = new ClinicSimulation(expected.doctorsState).AdvanceOffline(36 * 3600, ClinicRules.MaximumOfflineSeconds, ClinicRules.OfflineCoinCap(expected.doctorsState, 1));
            var report = store.ApplyOffline(now.AddHours(36));
            Assert.That(report.applied, Is.True, store.Error);
            Assert.That(report.earningsSeconds, Is.EqualTo(first.EarningsSeconds).Within(.000001));
            Assert.That(report.tillEarned, Is.EqualTo(first.TillEarned + second.TillEarned));
            Assert.That(report.paymentsReceived, Is.EqualTo(first.PaymentsReceived + second.PaymentsReceived));
            Assert.That(JsonUtility.ToJson(store.Profile.state), Is.EqualTo(JsonUtility.ToJson(expected.state)));
            Assert.That(JsonUtility.ToJson(store.Profile.doctorsState), Is.EqualTo(JsonUtility.ToJson(expected.doctorsState)));
            var loaded = new ClinicProfileStore(directory); loaded.LoadClinic(now.AddHours(36));
            Assert.That(loaded.LastOfflineReport.applied, Is.False);
            Assert.That(JsonUtility.ToJson(loaded.Profile), Is.EqualTo(JsonUtility.ToJson(store.Profile)));
        }

        [Test] public void FailedDualOfflineCommitRetriesFromTheSameUnchangedPair()
        {
            var store = CompletedStarter(); Assert.That(store.OpenDoctorsClinic(now), Is.True, store.Error);
            var original = JsonUtility.ToJson(store.Profile);
            Directory.CreateDirectory(path + ".tmp");
            Assert.That(store.ApplyOffline(now.AddMinutes(8)).applied, Is.False);
            Assert.That(store.HasPendingOfflineProgress, Is.True);
            Assert.That(store.SelectLocation(ClinicLocation.StarterClinic, now), Is.False);
            Assert.That(JsonUtility.ToJson(store.Profile), Is.EqualTo(original));
            Directory.Delete(path + ".tmp");
            Assert.That(store.ApplyOffline(now.AddMinutes(8)).applied, Is.True, store.Error);
            Assert.That(store.ApplyOffline(now.AddMinutes(8)).applied, Is.False);
        }

        [Test] public void VersionThreeCampaignWithBothClinicsMigratesWithAnEmptyGemLedger()
        {
            var store = CompletedStarter(); Assert.That(store.OpenDoctorsClinic(now), Is.True, store.Error);
            var envelope = WriteVersionThree(store.Profile);
            var original = JsonUtility.FromJson<VersionThreeProfile>(JsonUtility.FromJson<Envelope>(envelope).payload);

            var migrated = new ClinicProfileStore(directory);
            var loaded = migrated.LoadClinic(now);
            Assert.That(migrated.Error, Is.Null);
            Assert.That(loaded.schemaVersion, Is.EqualTo(ClinicProfile.CurrentSchemaVersion));
            Assert.That(loaded.revision, Is.EqualTo(original.revision + 1));
            Assert.That(loaded.lastAccountedUtcTicks, Is.EqualTo(original.lastAccountedUtcTicks));
            Assert.That(loaded.activeLocation, Is.EqualTo(original.activeLocation));
            Assert.That(JsonUtility.ToJson(loaded.state), Is.EqualTo(JsonUtility.ToJson(original.state)), "Starter clinic money, staff and patients are untouched.");
            Assert.That(JsonUtility.ToJson(loaded.doctorsState), Is.EqualTo(JsonUtility.ToJson(original.additionalClinics.Single())), "Doctors clinic is untouched.");
            Assert.That(loaded.state.SchemaVersion, Is.EqualTo(3));
            Assert.That(loaded.state.RulesVersion, Is.EqualTo(3), "Prices keep the rules the campaign was played under.");
            AssertEmptyLedger(loaded.premium);
            Assert.That(File.ReadAllText(path + ".v3-before-migration-" + original.revision), Is.EqualTo(envelope));
            Assert.That(JsonUtility.ToJson(new ClinicProfileStore(directory).LoadClinic(now)), Is.EqualTo(JsonUtility.ToJson(loaded)));
        }

        [Test] public void VersionThreeFileCannotCarryGemsIntoVersionFour()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var payload = JsonUtility.ToJson(ToVersionThree(store.Profile));
            payload = payload.Substring(0, payload.Length - 1)
                + ",\"premium\":{\"gems\":900,\"gemsEarned\":0,\"gemsPurchased\":900,\"gemsSpent\":0,\"processedTransactionIds\":[\"t1\"],\"milestonesClaimed\":[\"m1\"],\"decorationsOwned\":[\"d1\"]}}";
            WriteEnvelope(payload);
            var loaded = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(loaded.schemaVersion, Is.EqualTo(ClinicProfile.CurrentSchemaVersion));
            AssertEmptyLedger(loaded.premium);
        }

        [Test] public void GemLedgerRoundTripsAndAnImbalancedSaveFallsBackToTheLastGoodCopy()
        {
            var store = new ClinicProfileStore(directory); var profile = store.LoadClinic(now);
            profile.premium.gemsEarned = 25; profile.premium.gemsPurchased = 100; profile.premium.gemsSpent = 40; profile.premium.gems = 85;
            profile.premium.processedTransactionIds.Add("2000000123456789");
            profile.premium.milestonesClaimed.Add("reception.tier.2");
            profile.premium.decorationsOwned.Add("fountain");
            Assert.That(store.Save(profile, now), Is.True, store.Error);
            Assert.That(store.Save(profile, now), Is.True, store.Error);
            var reloaded = new ClinicProfileStore(directory).LoadClinic(now);
            Assert.That(JsonUtility.ToJson(reloaded.premium), Is.EqualTo(JsonUtility.ToJson(profile.premium)));

            var tampered = Clone(reloaded); tampered.premium.gems = 10000;
            WriteEnvelope(JsonUtility.ToJson(tampered));
            var recovered = new ClinicProfileStore(directory);
            Assert.That(recovered.LoadClinic(now).premium.gems, Is.EqualTo(85));
            Assert.That(recovered.Error, Does.Contain("backup"));
        }

        [Test] public void GemLedgerRejectsNegativeUnbalancedOrDuplicateRecords()
        {
            Assert.That(new ClinicPremiumState().IsValid(), Is.True);
            Assert.That(new ClinicPremiumState { gems = 1 }.IsValid(), Is.False);
            Assert.That(new ClinicPremiumState { gems = -5, gemsSpent = 5 }.IsValid(), Is.False);
            Assert.That(new ClinicPremiumState { gemsEarned = 5, gemsSpent = 6, gems = -1 }.IsValid(), Is.False);
            var duplicate = new ClinicPremiumState(); duplicate.processedTransactionIds.Add("1"); duplicate.processedTransactionIds.Add("1");
            Assert.That(duplicate.IsValid(), Is.False);
            var empty = new ClinicPremiumState(); empty.milestonesClaimed.Add("");
            Assert.That(empty.IsValid(), Is.False);
            var missing = new ClinicPremiumState { decorationsOwned = null };
            Assert.That(missing.IsValid(), Is.False);
        }

        /// <summary>The exact 3.3 profile shape: identical to version 4 without the premium ledger.</summary>
        [Serializable] private sealed class VersionThreeProfile
        {
            public int schemaVersion = 3;
            public long revision;
            public ClinicState state;
            public System.Collections.Generic.List<ClinicState> additionalClinics = new System.Collections.Generic.List<ClinicState>();
            public ClinicLocation activeLocation;
            public ClinicPreferences preferences = new ClinicPreferences();
            public long lastAccountedUtcTicks;
        }
        private static VersionThreeProfile ToVersionThree(ClinicProfile profile)
        {
            // 3.3 only ever wrote rules 3.
            var copy = Clone(profile);
            copy.state.RulesVersion = 3;
            foreach (var clinic in copy.additionalClinics) clinic.RulesVersion = 3;
            return new VersionThreeProfile
            {
                revision = copy.revision, state = copy.state, additionalClinics = copy.additionalClinics,
                activeLocation = copy.activeLocation, preferences = copy.preferences, lastAccountedUtcTicks = copy.lastAccountedUtcTicks
            };
        }
        private string WriteVersionThree(ClinicProfile profile)
        {
            WriteEnvelope(JsonUtility.ToJson(ToVersionThree(profile)));
            if (File.Exists(path + ".backup")) File.Delete(path + ".backup");
            return File.ReadAllText(path);
        }
        private static void AssertEmptyLedger(ClinicPremiumState premium)
        {
            Assert.That(premium, Is.Not.Null);
            Assert.That(premium.gems + premium.gemsEarned + premium.gemsPurchased + premium.gemsSpent, Is.Zero);
            Assert.That(premium.processedTransactionIds, Is.Empty);
            Assert.That(premium.milestonesClaimed, Is.Empty);
            Assert.That(premium.decorationsOwned, Is.Empty);
        }

        private ClinicProfileStore CompletedStarter()
        {
            var store = new ClinicProfileStore(directory); store.LoadClinic(now);
            var sim = new ClinicSimulation(store.Profile.state);
            foreach (var room in sim.State.Rooms)
            {
                while (room.Tier < 3) { Buy(sim, ClinicRules.RenovationCost(sim.State, room.Kind), () => sim.Renovate(room.Kind)); sim.Advance(180); }
                foreach (UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
                    while (room.Level(track) < 6) Buy(sim, ClinicRules.UpgradeCost(sim.State, room.Kind, track), () => sim.Upgrade(room.Kind, track));
            }
            foreach (var role in new[] { ClinicStaffRole.Receptionist, ClinicStaffRole.Nurse })
                for (var id = 0; id < 2; id++)
                    while (ClinicRules.StationLevel(sim.State, role, id) < 6)
                    { var station = id; Buy(sim, ClinicRules.StationUpgradeCost(sim.State, role, id), () => sim.UpgradeStation(role, station)); }
            foreach (var staff in sim.State.Staff)
                while (staff.TrainingLevel < 6) Buy(sim, ClinicRules.StaffTrainingCost(sim.State, staff), () => sim.TrainStaff(staff.Id));
            foreach (var amenity in sim.State.Amenities)
                while (amenity.Level < 3) Buy(sim, ClinicRules.AmenityUpgradeCost(sim.State, amenity.Kind), () => sim.UpgradeAmenity(amenity.Kind));
            Fund(sim, 100000);
            Assert.That(ClinicRules.StarterCompletion(sim.State), Is.Empty);
            Assert.That(store.Save(store.Profile, now), Is.True, store.Error);
            return store;
        }
        private static void Buy(ClinicSimulation sim, long cost, Func<ClinicCommandResult> command)
        { Fund(sim, cost); var result = command(); Assert.That(result.Success, Is.True, result.Message); }
        private static void Fund(ClinicSimulation sim, long cost)
        {
            for (var cycle = 0; cycle < 10 && sim.State.Wallet < cost; cycle++)
            { sim.AdvanceOffline(8 * 3600); foreach (var desk in sim.State.ReceptionDesks) sim.Collect(desk.Id); sim.CollectVendingTips(); }
            Assert.That(sim.State.Wallet, Is.GreaterThanOrEqualTo(cost));
        }
        private static ClinicProfile Clone(ClinicProfile profile) => JsonUtility.FromJson<ClinicProfile>(JsonUtility.ToJson(profile));
        private void WriteEnvelope(string payload)
        {
            using (var hash = SHA256.Create()) File.WriteAllText(path, JsonUtility.ToJson(new Envelope
            { payload = payload, checksum = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload))) }));
        }
    }
}
