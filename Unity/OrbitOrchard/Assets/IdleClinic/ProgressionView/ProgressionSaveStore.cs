using System;
using System.IO;
using IdleClinic.Progression;
using UnityEngine;

namespace IdleClinic.ProgressionView
{
    [Serializable]
    public sealed class ProgressionSave
    {
        public long SavedAtUnixSeconds;
        public ProgressionSnapshot Snapshot;
    }

    /// <summary>One JSON file, written through a temporary file, with the previous save kept as a backup.</summary>
    public sealed class ProgressionSaveStore
    {
        private readonly string path;
        private string Backup => path + ".bak";
        private string Temporary => path + ".tmp";

        public ProgressionSaveStore(string path) { this.path = path; }

        public static ProgressionSaveStore AtDefaultLocation() =>
            new ProgressionSaveStore(Path.Combine(Application.persistentDataPath, "boss-progression.json"));

        public void Save(ProgressionSave save)
        {
            File.WriteAllText(Temporary, JsonUtility.ToJson(save));
            if (File.Exists(path)) File.Copy(path, Backup, true);
            if (File.Exists(path)) File.Delete(path);
            File.Move(Temporary, path);
        }

        public ProgressionSave Load() => Read(path) ?? Read(Backup);

        public void Delete()
        {
            foreach (var file in new[] { path, Backup, Temporary }) if (File.Exists(file)) File.Delete(file);
        }

        private static ProgressionSave Read(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                var save = JsonUtility.FromJson<ProgressionSave>(File.ReadAllText(file));
                return save != null && save.Snapshot != null && save.Snapshot.Rooms != null && save.Snapshot.Rooms.Length > 0 ? save : null;
            }
            catch (Exception) { return null; }
        }
    }
}
