using System;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Character style data shared with the Blender tools (Resources/ClinicCharacterStyles.json): the torso surface
    /// that badges and marks lie on, hair and trouser colours per appearance, and the headwear, bodywear and departure
    /// relief shapes. One file feeds both the game and the review renders, so they cannot drift apart.</summary>
    [Serializable]
    public sealed class ClinicCharacterStyles
    {
        public TorsoRing[] torso;
        public string[] hairRoles, trouserRoles;
        public StyleGroup[] headwear, bodywear;
        public ShapeList relief;

        [Serializable] public sealed class TorsoRing { public float y, halfWidth, halfDepth, front; }
        [Serializable] public sealed class StyleGroup { public string who; public int[] appearances; public bool patientsOnly; public StyleShape[] shapes; }
        [Serializable] public sealed class ShapeList { public StyleShape[] shapes; }
        [Serializable] public sealed class StyleShape { public string shape, name, role; public Vector3 position, size; }

        private const string Resource = "ClinicCharacterStyles";
        private static ClinicCharacterStyles loaded;

        public static ClinicCharacterStyles Current
        {
            get
            {
                if (loaded != null) return loaded;
                var text = Resources.Load<TextAsset>(Resource);
                loaded = text == null ? new ClinicCharacterStyles() : JsonUtility.FromJson<ClinicCharacterStyles>(text.text) ?? new ClinicCharacterStyles();
                if (text == null) Debug.LogError("Missing " + Resource + " character styles.");
                loaded.torso = loaded.torso ?? Array.Empty<TorsoRing>();
                loaded.hairRoles = loaded.hairRoles ?? Array.Empty<string>();
                loaded.trouserRoles = loaded.trouserRoles ?? Array.Empty<string>();
                loaded.headwear = loaded.headwear ?? Array.Empty<StyleGroup>();
                loaded.bodywear = loaded.bodywear ?? Array.Empty<StyleGroup>();
                loaded.relief = loaded.relief ?? new ShapeList();
                loaded.relief.shapes = loaded.relief.shapes ?? Array.Empty<StyleShape>();
                return loaded;
            }
        }

        /// <summary>Hair colour role for an appearance.</summary>
        public string Hair(int appearance) => hairRoles.Length == 0 ? "Hair" : hairRoles[Math.Abs(appearance) % hairRoles.Length];

        /// <summary>Trouser colour role for a visitor's appearance.</summary>
        public string Trousers(int appearance) => trouserRoles.Length == 0 ? "Trousers" : trouserRoles[Math.Abs(appearance) % trouserRoles.Length];

        /// <summary>Depth of the torso's front surface (local z) at height y and sideways offset x, in the standing pose.</summary>
        public float TorsoFront(float x, float y)
        {
            for (int i = 0; i + 1 < torso.Length; i++)
            {
                var a = torso[i]; var b = torso[i + 1];
                if (y < a.y || y > b.y) continue;
                float t = (y - a.y) / Math.Max(1e-5f, b.y - a.y);
                float width = Mathf.Lerp(a.halfWidth, b.halfWidth, t), depth = Mathf.Lerp(a.halfDepth, b.halfDepth, t) + Mathf.Lerp(a.front, b.front, t);
                return depth * Mathf.Sqrt(Mathf.Max(0, 1 - (x / width) * (x / width)));
            }
            return 0;
        }

        /// <summary>True when the group applies to this appearance (an empty list means every appearance).</summary>
        public static bool Includes(StyleGroup group, int appearance)
        {
            if (group.appearances == null || group.appearances.Length == 0) return true;
            foreach (var value in group.appearances) if (value == appearance) return true;
            return false;
        }

        /// <summary>Build one shape under parent; roles starting with @ take the hair colour.</summary>
        internal static GameObject Build(ClinicArt art, Transform parent, StyleShape shape, string hairRole)
        {
            var role = !string.IsNullOrEmpty(shape.role) && shape.role[0] == '@' ? hairRole : shape.role;
            return shape.shape == "Box" ? art.Box(shape.name, parent, shape.position, shape.size, role) : art.Orb(shape.name, parent, shape.position, shape.size, role);
        }
    }
}
