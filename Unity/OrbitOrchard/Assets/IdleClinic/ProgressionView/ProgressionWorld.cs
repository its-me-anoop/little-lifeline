using System.Collections.Generic;
using IdleClinic.Progression;
using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>Lays out the rooms, the boss and the waiting patients, and keeps them matched to the rules.</summary>
    internal sealed class ProgressionWorld
    {
        private const float Gap = 1.6f;
        private readonly ClinicProgression game;
        private readonly Dictionary<RoomId, RoomView> rooms = new Dictionary<RoomId, RoomView>();
        private readonly BossView boss;
        private readonly List<GameObject> queue = new List<GameObject>();
        private readonly Transform root;
        private readonly HashSet<RoomId> treating = new HashSet<RoomId>();
        private Vector3 walkFrom;
        private bool wasTravelling;

        public IEnumerable<RoomView> Rooms => rooms.Values;
        public RoomView Room(RoomId id) => rooms[id];
        public Bounds Bounds { get; }
        public Vector3 BossPosition => boss.Position;

        public ProgressionWorld(ClinicProgression game)
        {
            this.game = game;
            root = new GameObject("Little Lifeline progression").transform;
            var step = RoomView.Size + Gap;
            // Two columns, three rows, with a corridor down the middle and between the rows.
            var layout = new Dictionary<RoomId, Vector2>
            {
                { RoomId.Office, new Vector2(-1, 1) }, { RoomId.Reception, new Vector2(1, 1) },
                { RoomId.NursingStation1, new Vector2(-1, 0) }, { RoomId.Waiting, new Vector2(1, 0) },
                { RoomId.NursingStation2, new Vector2(-1, -1) }, { RoomId.Parking, new Vector2(1, -1) },
            };
            foreach (var definition in RoomCatalog.All)
            {
                var cell = layout[definition.Id];
                rooms[definition.Id] = new RoomView(game.State, definition.Id, new Vector3(cell.x * step * 0.5f, 0, cell.y * step), root);
            }
            Bounds = new Bounds(new Vector3(0, 0, 0), new Vector3(step + RoomView.Size, 1, step * 2 + RoomView.Size));
            var ground = Shapes.Box(root, new Vector3(0, -0.3f, 0), new Vector3(step + RoomView.Size + 3, 0.2f, step * 2 + RoomView.Size + 3), new Color(.78f, .84f, .72f), "Ground");
            ground.name = "Ground";
            boss = new BossView(root, rooms[RoomId.Office].WorkSpot, 0);
            walkFrom = rooms[RoomId.Office].WorkSpot;
            game.Occurred += OnEvent;
        }

        private void OnEvent(ProgressionEvent e)
        {
            if (e.Kind == ProgressionEventKind.TreatmentStarted) treating.Add(e.Room);
            if (e.Kind == ProgressionEventKind.TreatmentCompleted) treating.Remove(e.Room);
        }

        public void Refresh()
        {
            var task = game.BossTask;
            foreach (var view in rooms.Values)
            {
                var mine = task != null && task.Value.Room == view.Id;
                var cleaning = mine && task.Value.Kind == BossTaskKind.Clean ? game.BossProgress : 0;
                var building = mine && (task.Value.Kind == BossTaskKind.LevelUp || task.Value.Kind == BossTaskKind.UpgradeFurniture) ? game.BossProgress : 0;
                view.Refresh(game.State.IsUnlocked(view.Id), cleaning, building);
                view.PatientOnBed = treating.Contains(view.Id);
            }

            if (game.BossTravelling && game.BossDestination != null)
            {
                if (!wasTravelling) walkFrom = rooms[game.BossLocation].WorkSpot;
                boss.Travelling();
                boss.Walk(walkFrom, rooms[game.BossDestination.Value].WorkSpot, game.BossTravelProgress);
            }
            else
            {
                boss.Show(task?.Kind);
                boss.Stand(rooms[game.BossLocation].WorkSpot, game.BossProgress, task != null);
            }
            wasTravelling = game.BossTravelling;
            RefreshQueue();
        }

        private void RefreshQueue()
        {
            var home = rooms[game.State.IsUnlocked(RoomId.Waiting) && game.State.Room(RoomId.Waiting).IsClean ? RoomId.Waiting : RoomId.Reception];
            var shown = Mathf.Min(game.State.PatientsWaiting, 8);
            while (queue.Count < shown)
            {
                var models = new[] { "Patient_01_Man_Coral", "Patient_02_Woman_Lavender", "Patient_03_Man_Mint", "Patient_04_Woman_Sunflower", "Patient_05_OlderMan_Blue", "Patient_06_OlderWoman_Rose" };
                var patient = Models.Spawn(models[queue.Count % models.Length], root, Vector3.zero, 0, false);
                Models.Play(patient, "Idle");
                queue.Add(patient);
            }
            for (var i = 0; i < queue.Count; i++)
            {
                queue[i].SetActive(i < shown);
                var column = i % 4; var row = i / 4;
                queue[i].transform.position = home.Root.position + new Vector3(-1.6f + column * 1.05f, 0, -0.4f - row * 0.9f);
            }
        }
    }
}
