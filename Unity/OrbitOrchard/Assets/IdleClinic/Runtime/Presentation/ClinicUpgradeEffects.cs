using System.Collections.Generic;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Makes improvements visible as they happen: new fittings grow into place and a ring of sparkles
    /// rises from the room. Presentation only; quiet on the first frame after loading and with less motion.</summary>
    internal sealed class ClinicUpgradeEffects
    {
        /// <summary>The effects of the world currently on screen; rebuilt with the scene.</summary>
        internal static ClinicUpgradeEffects Current { get; private set; }
        private const float PopSeconds = .45f, BurstSeconds = .95f;
        private const int Sparkles = 10, Bursts = 3;
        private sealed class Pop { internal Transform Target; internal Vector3 Scale; internal float Age; }
        private sealed class Burst { internal Transform[] Sparks; internal Vector3 Origin; internal float Radius, Age = -1; }
        private readonly List<Pop> pops = new List<Pop>();
        private readonly Burst[] bursts = new Burst[Bursts];
        private int nextBurst;
        internal bool Armed { get; set; }
        internal bool Reduced { get; set; }

        internal ClinicUpgradeEffects(ClinicArt art, Transform parent)
        {
            var root = art.Group("Upgrade celebration", parent);
            for (var b = 0; b < Bursts; b++)
            {
                var burst = new Burst { Sparks = new Transform[Sparkles] };
                for (var i = 0; i < Sparkles; i++)
                {
                    burst.Sparks[i] = art.Orb("Upgrade sparkle", root, Vector3.zero, Vector3.one * .16f, i % 3 == 0 ? "LampLight" : "Gold").transform;
                    burst.Sparks[i].gameObject.SetActive(false);
                }
                bursts[b] = burst;
            }
            Current = this;
        }

        /// <summary>Show or hide a fitting; one that appears during play grows into place.</summary>
        internal static void Show(GameObject target, bool active)
        {
            if (target.activeSelf == active) return;
            target.SetActive(active);
            var effects = Current;
            if (!active || effects == null || !effects.Armed || effects.Reduced) return;
            foreach (var pop in effects.pops) if (pop.Target == target.transform) return;
            effects.pops.Add(new Pop { Target = target.transform, Scale = target.transform.localScale, Age = 0 });
            target.transform.localScale = target.transform.localScale * .01f;
        }

        /// <summary>A ring of sparkles rising from the floor around <paramref name="point"/>.</summary>
        internal void Celebrate(Vector3 point, float radius)
        {
            if (Reduced) return;
            var burst = bursts[nextBurst]; nextBurst = (nextBurst + 1) % Bursts;
            burst.Origin = point; burst.Radius = radius; burst.Age = 0;
            foreach (var spark in burst.Sparks) spark.gameObject.SetActive(true);
        }

        internal void Update(float delta)
        {
            for (var i = pops.Count - 1; i >= 0; i--)
            {
                var pop = pops[i]; pop.Age += delta;
                var t = Mathf.Clamp01(pop.Age / PopSeconds);
                if (pop.Target == null) { pops.RemoveAt(i); continue; }
                pop.Target.localScale = pop.Scale * Mathf.Lerp(.01f, 1f, 1 - Mathf.Pow(1 - t, 4));
                if (t >= 1 || Reduced) { pop.Target.localScale = pop.Scale; pops.RemoveAt(i); }
            }
            foreach (var burst in bursts)
            {
                if (burst.Age < 0) continue;
                burst.Age += delta;
                var t = Mathf.Clamp01(burst.Age / BurstSeconds);
                var eased = 1 - Mathf.Pow(1 - t, 3);
                for (var i = 0; i < Sparkles; i++)
                {
                    var angle = (i / (float)Sparkles + t * .15f) * Mathf.PI * 2;
                    var distance = Mathf.Lerp(.3f, burst.Radius, eased);
                    burst.Sparks[i].position = burst.Origin + new Vector3(Mathf.Cos(angle) * distance, .2f + eased * 1.4f + (i % 2) * .15f, Mathf.Sin(angle) * distance);
                    burst.Sparks[i].localScale = Vector3.one * .18f * (1 - t);
                }
                if (t >= 1 || Reduced)
                {
                    burst.Age = -1;
                    foreach (var spark in burst.Sparks) spark.gameObject.SetActive(false);
                }
            }
        }
    }
}
