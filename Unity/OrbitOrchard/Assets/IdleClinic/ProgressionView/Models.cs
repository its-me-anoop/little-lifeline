using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>Loads the imported clinic models, stands them on the floor and recentres them on their footprint.</summary>
    internal static class Models
    {
        private const string Folder = "Progression/Models/";

        public static GameObject Spawn(string model, Transform parent, Vector3 localPosition, float yaw = 0, bool recentre = true)
        {
            var prefab = Resources.Load<GameObject>(Folder + model);
            if (prefab == null) { Debug.LogWarning("Missing model " + model); return Shapes.Empty(parent, localPosition, model + " (missing)"); }
            var holder = Shapes.Empty(parent, localPosition, model);
            holder.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var instance = Object.Instantiate(prefab, holder.transform, false);
            instance.name = "Model";
            // Characters are authored standing on the origin; skinned bounds are unreliable before the first frame.
            if (!recentre) return holder;
            var bounds = BoundsOf(instance);
            // Authored room layouts sit away from the origin; centre them and drop them onto y = 0.
            // Move until the footprint's centre sits on the holder; measured in the world so any import scale or axis fix is covered.
            for (var pass = 0; pass < 3; pass++)
            {
                bounds = BoundsOf(instance);
                var offset = holder.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                if (offset.sqrMagnitude < 1e-6f) break;
                instance.transform.position += offset;
            }
            return holder;
        }

        public static Bounds BoundsOf(GameObject go)
        {
            var bounds = new Bounds(go.transform.position, Vector3.zero); var first = true;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        /// <summary>Plays a named legacy clip on looping repeat; harmless when the model has none.</summary>
        public static void Play(GameObject holder, string clip, float fade = 0.15f)
        {
            var animation = holder.GetComponentInChildren<Animation>();
            if (animation == null || animation[clip] == null) return;
            animation[clip].wrapMode = WrapMode.Loop;
            animation.CrossFade(clip, fade);
        }
    }
}
