using System;
using IdleClinic.Progression;

namespace IdleClinic.ProgressionView
{
    /// <summary>Loads a saved game, catches it up for time away, and saves it again. The clock is injected so it can be tested.</summary>
    public sealed class ProgressionSession
    {
        private readonly ProgressionSaveStore store;
        private readonly Func<long> now;
        private readonly ProgressionSettings settings;
        private readonly double limitSeconds;
        private long lastSeen;

        public ClinicProgression Game { get; private set; }
        public OfflineReport Report { get; private set; }

        public ProgressionSession(ProgressionSaveStore store, Func<long> now, ProgressionSettings settings, double limitSeconds)
        {
            this.store = store; this.now = now; this.settings = settings; this.limitSeconds = limitSeconds;
            Game = new ClinicProgression(settings);
            lastSeen = now();
            var saved = store.Load();
            if (saved == null) return;
            Game.Restore(saved.Snapshot);
            Report = OfflineProgress.Advance(Game, now() - saved.SavedAtUnixSeconds, limitSeconds);
        }

        public void Save()
        {
            lastSeen = now();
            store.Save(new ProgressionSave { SavedAtUnixSeconds = lastSeen, Snapshot = Game.Capture() });
        }

        /// <summary>The app came back from the background: run the game forward for the time it was away.</summary>
        public OfflineReport Resume()
        {
            var report = OfflineProgress.Advance(Game, now() - lastSeen, limitSeconds);
            Save();
            return report;
        }

        public void Reset()
        {
            store.Delete();
            Game = new ClinicProgression(settings);
            Report = default;
            lastSeen = now();
        }
    }
}
