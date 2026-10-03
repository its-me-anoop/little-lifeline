using IdleClinic.Progression;
using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>The boss: a suited capsule with a broom or hammer, walking between rooms along the corridors.</summary>
    internal sealed class BossView
    {
        private readonly Transform root;
        private readonly GameObject broom, hammer;
        private readonly float corridorX;
        private Vector3 resting;

        public Vector3 Position => root.position;

        public BossView(Transform parent, Vector3 start, float corridorX)
        {
            this.corridorX = corridorX;
            root = Shapes.Empty(parent, start, "Boss").transform;
            Shapes.Make(PrimitiveType.Capsule, root, new Vector3(0, 0.75f, 0), new Vector3(0.6f, 0.75f, 0.6f), new Color(.15f, .2f, .35f), "Suit");
            Shapes.Make(PrimitiveType.Cube, root, new Vector3(0, 0.95f, 0.29f), new Vector3(0.1f, 0.4f, 0.04f), new Color(.85f, .2f, .25f), "Tie");
            Shapes.Make(PrimitiveType.Sphere, root, new Vector3(0, 1.7f, 0), Vector3.one * 0.5f, new Color(.95f, .8f, .65f), "Head");
            broom = Shapes.Empty(root, new Vector3(0.5f, 0, 0.2f), "Broom");
            Shapes.Make(PrimitiveType.Cylinder, broom.transform, new Vector3(0, 0.8f, 0), new Vector3(0.06f, 0.8f, 0.06f), new Color(.8f, .65f, .3f));
            Shapes.Box(broom.transform, new Vector3(0, 0.05f, 0), new Vector3(0.5f, 0.12f, 0.15f), new Color(.9f, .8f, .35f));
            hammer = Shapes.Empty(root, new Vector3(0.5f, 0.5f, 0.2f), "Hammer");
            Shapes.Make(PrimitiveType.Cylinder, hammer.transform, new Vector3(0, 0.3f, 0), new Vector3(0.07f, 0.4f, 0.07f), new Color(.5f, .35f, .2f));
            Shapes.Box(hammer.transform, new Vector3(0, 0.7f, 0), new Vector3(0.35f, 0.18f, 0.18f), new Color(.5f, .5f, .55f));
            resting = start;
            Show(null);
        }

        public void Show(BossTaskKind? working)
        {
            broom.SetActive(working == BossTaskKind.Clean);
            hammer.SetActive(working == BossTaskKind.LevelUp || working == BossTaskKind.UpgradeFurniture);
        }

        public void Stand(Vector3 spot, double workProgress, bool working)
        {
            resting = spot;
            var sway = working ? Mathf.Sin((float)workProgress * Mathf.PI * 14) : 0;
            root.position = spot + new Vector3(sway * 0.35f, working ? Mathf.Abs(sway) * 0.08f : 0, 0);
            root.rotation = Quaternion.Euler(0, working ? 0 : 180, 0);
        }

        /// <summary>Corridor route: out to the middle corridor, along it, then into the target room.</summary>
        public void Walk(Vector3 from, Vector3 to, double progress)
        {
            var p0 = from; var p1 = new Vector3(corridorX, 0, from.z);
            var p2 = new Vector3(corridorX, 0, to.z); var p3 = to;
            var l1 = Vector3.Distance(p0, p1); var l2 = Vector3.Distance(p1, p2); var l3 = Vector3.Distance(p2, p3);
            var along = (float)progress * (l1 + l2 + l3);
            Vector3 a, b; float t;
            if (along < l1) { a = p0; b = p1; t = l1 > 0 ? along / l1 : 1; }
            else if (along < l1 + l2) { a = p1; b = p2; t = l2 > 0 ? (along - l1) / l2 : 1; }
            else { a = p2; b = p3; t = l3 > 0 ? (along - l1 - l2) / l3 : 1; }
            var position = Vector3.Lerp(a, b, t);
            var heading = b - a;
            if (heading.sqrMagnitude > 0.0001f) root.rotation = Quaternion.LookRotation(heading);
            root.position = position + new Vector3(0, Mathf.Abs(Mathf.Sin((float)progress * 24)) * 0.15f, 0);
        }
    }
}
