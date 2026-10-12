using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Services
{
    /// <summary>Reads the opening story (Resources/ClinicStory.json) once. A missing or damaged file gives an empty script,
    /// and the opening then plays exactly as it did before the story.</summary>
    public static class ClinicStoryScripts
    {
        public const string Resource = "ClinicStory";
        private static ClinicStoryScript loaded;

        public static ClinicStoryScript Current
        {
            get
            {
                if (loaded != null) return loaded;
                var text = Resources.Load<TextAsset>(Resource);
                if (text == null) Debug.LogError("Missing " + Resource + " story script.");
                loaded = Parse(text == null ? null : text.text);
                return loaded;
            }
        }

        public static ClinicStoryScript Parse(string json)
        {
            ClinicStoryScript script = null;
            if (!string.IsNullOrEmpty(json))
                try { script = JsonUtility.FromJson<ClinicStoryScript>(json); }
                catch (System.ArgumentException) { script = null; }
            return (script ?? new ClinicStoryScript()).Normalized();
        }
    }
}
