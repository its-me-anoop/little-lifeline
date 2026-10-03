using System.Linq;
using System.IO;
using IdleClinic.Progression;
using NUnit.Framework;

namespace IdleClinic.ProgressionView.Tests
{
    public sealed class ProgressionSaveStoreTests
    {
        private string folder;
        private string path;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "lifeline-save-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "progression.json");
        }

        [TearDown]
        public void TearDown() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }

        private static ProgressionSave Played(double seconds, long savedAt)
        {
            var game = new ClinicProgression(new ProgressionSettings());
            game.Tick(seconds);
            return new ProgressionSave { SavedAtUnixSeconds = savedAt, Snapshot = game.Capture() };
        }

        [Test]
        public void NothingSavedLoadsAsNull()
        {
            Assert.IsNull(new ProgressionSaveStore(path).Load());
        }

        [Test]
        public void ASavedGameComesBackWhole()
        {
            var store = new ProgressionSaveStore(path);
            var save = Played(500, 1700000000);
            store.Save(save);
            var loaded = store.Load();
            Assert.AreEqual(1700000000, loaded.SavedAtUnixSeconds);
            Assert.AreEqual(save.Snapshot.Wallet, loaded.Snapshot.Wallet);
            Assert.AreEqual(save.Snapshot.UpgradesPurchased, loaded.Snapshot.UpgradesPurchased);
            Assert.AreEqual(save.Snapshot.Rooms.Length, loaded.Snapshot.Rooms.Length);
        }

        [Test]
        public void ACorruptSaveFallsBackToTheLastGoodOne()
        {
            var store = new ProgressionSaveStore(path);
            store.Save(Played(100, 1));
            store.Save(Played(600, 2));
            File.WriteAllText(path, "{ this is not a save");
            var loaded = store.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.SavedAtUnixSeconds);
        }

        [Test]
        public void ACorruptSaveWithNoBackupLoadsAsNull()
        {
            File.WriteAllText(path, "garbage");
            Assert.IsNull(new ProgressionSaveStore(path).Load());
        }

        [Test]
        public void DeleteStartsOver()
        {
            var store = new ProgressionSaveStore(path);
            store.Save(Played(100, 1));
            store.Save(Played(200, 2));
            store.Delete();
            Assert.IsNull(store.Load());
        }

        [Test]
        public void SavingLeavesNoHalfWrittenFileBehind()
        {
            var store = new ProgressionSaveStore(path);
            store.Save(Played(100, 1));
            CollectionAssert.AreEquivalent(new[] { "progression.json" }, Directory.GetFiles(folder).Select(Path.GetFileName));
        }
    }
}
