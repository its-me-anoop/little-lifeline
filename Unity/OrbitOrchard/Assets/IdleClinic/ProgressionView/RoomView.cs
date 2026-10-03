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

        public readonly RoomId Id;
        public readonly Transform Root;
        private readonly RoomState room;
        private readonly Renderer floor;
        private readonly GameObject[] dust;
        private readonly GameObject boxes;
        private readonly GameObject fitting;
        private readonly System.Collections.Generic.List<GameObject> parts;
        private readonly System.Collections.Generic.List<GameObject> seats = new System.Collections.Generic.List<GameObject>();
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

            var spec = Specs[id];
            fitting = Models.Spawn(spec.Model, Root, spec.Offset, spec.Yaw);
            fitting.transform.localScale = Vector3.one * spec.Scale;
            parts = new System.Collections.Generic.List<GameObject>();
            foreach (Transform piece in fitting.transform.Find("Model")) parts.Add(piece.gameObject);

            if (id == RoomId.Waiting) BuildSeats();
            if (id == RoomId.Parking) { fitting.SetActive(false); }

            if (definition.Staff != StaffRole.None)
            {
                var model = definition.Staff == StaffRole.Receptionist ? "Receptionist_01_Man_NavySuit"
                    : id == RoomId.NursingStation1 ? "Nurse_02_Woman_BlueScrubs" : "Nurse_01_Man_TealScrubs";
                staff = Models.Spawn(model, Root, spec.Staff, spec.StaffYaw, false);
                Models.Play(staff, "Idle");
            }
            if (definition.Staff == StaffRole.Nurse)
            {
                patient = Models.Spawn("Patient_01_Man_Coral", Root, spec.Patient, spec.PatientYaw, false);
                Models.Play(patient, "Sit");
                patient.SetActive(false);
            }
        }

        private struct Spec
        {
            public string Model; public Vector3 Offset; public float Yaw, Scale;
            public Vector3 Staff; public float StaffYaw; public Vector3 Patient; public float PatientYaw;
        }

        private static readonly System.Collections.Generic.Dictionary<RoomId, Spec> Specs = new System.Collections.Generic.Dictionary<RoomId, Spec>
        {
            { RoomId.Office, new Spec { Model = "BossOffice_Furniture", Offset = new Vector3(0, 0, 0.4f), Yaw = 0, Scale = 1 } },
            { RoomId.Reception, new Spec { Model = "ReceptionDesk_TwoWorkstations", Offset = new Vector3(0, 0, 1.2f), Yaw = 0, Scale = 1, Staff = new Vector3(-0.6f, 0, 2.0f), StaffYaw = 180 } },
            { RoomId.NursingStation1, new Spec { Model = "NursingStation_1", Offset = new Vector3(0, 0, 0.6f), Yaw = 0, Scale = 1, Staff = new Vector3(0.4f, 0, 1.3f), StaffYaw = 180, Patient = new Vector3(-0.9f, 0.5f, 0.4f), PatientYaw = 0 } },
            { RoomId.Waiting, new Spec { Model = "WaitingChair", Offset = new Vector3(0, 0, 1.4f), Yaw = 0, Scale = 1 } },
            { RoomId.NursingStation2, new Spec { Model = "NursingStation_2", Offset = new Vector3(0, 0, 0.6f), Yaw = 0, Scale = 1, Staff = new Vector3(0.4f, 0, 1.3f), StaffYaw = 180, Patient = new Vector3(-0.9f, 0.5f, 0.4f), PatientYaw = 0 } },
            { RoomId.Parking, new Spec { Model = "ParkingBarrier", Offset = new Vector3(0, 0, 1.6f), Yaw = 0, Scale = 1 } },
        };

        private void BuildSeats()
        {
            var original = fitting;
            for (var i = 0; i < 6; i++)
            {
                var seat = Models.Spawn("WaitingChair", Root, new Vector3(-1.6f + (i % 3) * 1.6f, 0, 1.7f - (i / 3) * 1.5f), 180);
                seats.Add(seat);
            }
            original.SetActive(false);
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
            fitting.SetActive(built && Id != RoomId.Waiting && Id != RoomId.Parking);
            // Level one shows the basics; every furniture upgrade adds more of the authored pieces.
            var sum = 0; foreach (var f in room.FurnitureLevels) sum += f;
            var share = room.Level < 2 ? 0.4f + 0.6f * sum / (room.FurnitureLevels.Length * Mathf.Max(1, room.Level)) : 1f;
            for (var i = 0; i < parts.Count; i++) parts[i].SetActive(i < Mathf.CeilToInt(parts.Count * share));
            for (var i = 0; i < seats.Count; i++) seats[i].SetActive(built && i < 2 + sum * 2 + (room.Level >= 2 ? 6 : 0));
            if (Id == RoomId.Parking) fitting.SetActive(built);
            if (staff != null) staff.SetActive(room.StaffHired);
        }
    }
}
