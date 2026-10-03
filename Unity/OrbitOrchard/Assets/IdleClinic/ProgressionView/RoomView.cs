using IdleClinic.Progression;
using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>One room in the world. Reads the room's state each frame and shows it: dust, boxes, floor, furniture, staff.</summary>
    internal sealed class RoomView
    {
        public const float Size = 5f;
        private static readonly Color Cardboard = new Color(.72f, .52f, .3f);
        private static readonly Color Dust = new Color(.55f, .52f, .47f);
        private static readonly Color[] Floors =
        {
            new Color(.45f, .44f, .43f), new Color(.62f, .8f, .72f), new Color(.55f, .72f, .9f),
            new Color(.76f, .66f, .9f), new Color(.95f, .78f, .52f), new Color(.95f, .65f, .7f)
        };
        private static readonly Color[] Pieces =
        {
            new Color(.6f, .45f, .3f), new Color(.2f, .6f, .5f), new Color(.2f, .45f, .85f),
            new Color(.55f, .3f, .8f), new Color(.9f, .55f, .15f), new Color(.85f, .25f, .45f)
        };

        public readonly RoomId Id;
        public readonly Transform Root;
        private readonly RoomState room;
        private readonly Renderer floor;
        private readonly GameObject[] dust;
        private readonly GameObject boxes;
        private readonly GameObject[] furniture;
        private readonly GameObject staff;
        private readonly GameObject patient;

        public Vector3 WorkSpot => Root.position + new Vector3(0, 0, -1.4f);
        public Vector3 LabelSpot => Root.position + new Vector3(0, 1.4f, Size * 0.5f);
        public bool PatientOnBed { set { if (patient != null) patient.SetActive(value); } }

        public RoomView(ProgressionState state, RoomId id, Vector3 position, Transform parent)
        {
            Id = id; room = state.Room(id);
            var definition = RoomCatalog.Definition(id);
            Root = Shapes.Empty(parent, position, definition.Name).transform;
            floor = Shapes.Box(Root, new Vector3(0, -0.1f, 0), new Vector3(Size, 0.2f, Size), Floors[0], "Floor").GetComponent<Renderer>();
            var wall = new Color(.9f, .88f, .83f);
            Shapes.Box(Root, new Vector3(0, 0.45f, Size * 0.5f), new Vector3(Size, 0.9f, 0.15f), wall, "Back wall");
            var outer = position.x < 0 ? -1 : 1;
            Shapes.Box(Root, new Vector3(outer * Size * 0.5f, 0.45f, 0), new Vector3(0.15f, 0.9f, Size), wall, "Outer wall");

            dust = new GameObject[9];
            var seed = new System.Random((int)id * 31 + 7);
            for (var i = 0; i < dust.Length; i++)
            {
                var spot = new Vector3((float)(seed.NextDouble() - .5) * Size * .8f, 0.04f, (float)(seed.NextDouble() - .5) * Size * .8f);
                var width = 0.5f + (float)seed.NextDouble() * 0.7f;
                dust[i] = Shapes.Make(PrimitiveType.Sphere, Root, spot, new Vector3(width, 0.08f, width * 0.8f), Dust, "Dust");
            }
            boxes = Shapes.Empty(Root, Vector3.zero, "Empty boxes");
            for (var i = 0; i < 3; i++) BuildOpenBox(boxes.transform, new Vector3(-1.5f + i * 1.4f, 0, 1.1f - (i % 2) * 0.9f), 15f * i);

            furniture = new GameObject[definition.Furniture.Length];
            for (var i = 0; i < furniture.Length; i++)
                furniture[i] = Shapes.Box(Root, new Vector3(i == 0 ? -1.1f : 1.1f, 0.3f, 1.5f), new Vector3(1f, 0.6f, 0.8f), Pieces[0], definition.Furniture[i]);

            if (definition.Staff != StaffRole.None)
            {
                staff = Shapes.Empty(Root, new Vector3(0.2f, 0, 0.3f), "Staff");
                var tint = definition.Staff == StaffRole.Receptionist ? new Color(.2f, .6f, .65f) : new Color(.9f, .45f, .6f);
                Shapes.Make(PrimitiveType.Capsule, staff.transform, new Vector3(0, 0.6f, 0), new Vector3(0.45f, 0.6f, 0.45f), tint, "Body");
                Shapes.Make(PrimitiveType.Sphere, staff.transform, new Vector3(0, 1.4f, 0), Vector3.one * 0.38f, new Color(.95f, .8f, .65f), "Head");
            }
            if (definition.Staff == StaffRole.Nurse)
            {
                patient = Shapes.Empty(Root, new Vector3(-1.1f, 0.75f, 1.5f), "Patient");
                Shapes.Make(PrimitiveType.Capsule, patient.transform, Vector3.zero, new Vector3(0.4f, 0.45f, 0.4f), new Color(.95f, .6f, .5f), "Patient body").transform.localRotation = Quaternion.Euler(0, 0, 90);
                patient.SetActive(false);
            }
        }

        private static void BuildOpenBox(Transform parent, Vector3 at, float yaw)
        {
            var box = Shapes.Empty(parent, at, "Box");
            box.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            Shapes.Box(box.transform, new Vector3(0, 0.03f, 0), new Vector3(0.8f, 0.06f, 0.8f), Cardboard * 0.9f);
            Shapes.Box(box.transform, new Vector3(0, 0.3f, 0.4f), new Vector3(0.8f, 0.6f, 0.05f), Cardboard);
            Shapes.Box(box.transform, new Vector3(0, 0.3f, -0.4f), new Vector3(0.8f, 0.6f, 0.05f), Cardboard);
            Shapes.Box(box.transform, new Vector3(0.4f, 0.3f, 0), new Vector3(0.05f, 0.6f, 0.8f), Cardboard);
            Shapes.Box(box.transform, new Vector3(-0.4f, 0.3f, 0), new Vector3(0.05f, 0.6f, 0.8f), Cardboard);
            var flapA = Shapes.Box(box.transform, new Vector3(0, 0.75f, 0.62f), new Vector3(0.8f, 0.04f, 0.4f), Cardboard * 1.1f);
            flapA.transform.localRotation = Quaternion.Euler(-50, 0, 0);
            var flapB = Shapes.Box(box.transform, new Vector3(0, 0.75f, -0.62f), new Vector3(0.8f, 0.04f, 0.4f), Cardboard * 1.1f);
            flapB.transform.localRotation = Quaternion.Euler(50, 0, 0);
        }

        /// <param name="cleaning">0 to 1 while the boss is cleaning this room, otherwise 0.</param>
        public void Refresh(bool unlocked, double cleaning, double building)
        {
            var dustLeft = room.IsClean ? 0f : (float)(1 - cleaning);
            foreach (var d in dust) d.SetActive(dustLeft > 0.02f);
            boxes.SetActive(!room.IsClean && cleaning < 0.7);
            var tint = unlocked ? Floors[Mathf.Min(room.Level, Floors.Length - 1)] : new Color(.25f, .26f, .3f);
            if (room.Level == 0 && unlocked) tint = Color.Lerp(Floors[0], new Color(.75f, .72f, .66f), room.IsClean ? 1 : (float)cleaning);
            floor.sharedMaterial.color = tint;
            var bounce = building > 0 ? 1 + 0.04f * Mathf.Sin((float)building * Mathf.PI * 6) : 1;
            Root.localScale = new Vector3(1, bounce, 1);

            var built = room.Level >= 1;
            for (var i = 0; i < furniture.Length; i++)
            {
                furniture[i].SetActive(built);
                var level = room.FurnitureLevels[i];
                furniture[i].transform.localScale = new Vector3(0.9f + 0.12f * level, 0.5f + 0.18f * level, 0.7f + 0.06f * level);
                furniture[i].transform.localPosition = new Vector3(i == 0 ? -1.1f : 1.1f, furniture[i].transform.localScale.y * 0.5f, 1.5f);
                furniture[i].GetComponent<Renderer>().sharedMaterial.color = Pieces[Mathf.Min(level, Pieces.Length - 1)];
            }
            if (staff != null) staff.SetActive(room.StaffHired);
        }
    }
}
