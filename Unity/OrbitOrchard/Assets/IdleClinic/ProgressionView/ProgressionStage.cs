using System.Collections.Generic;
using System.Globalization;
using IdleClinic.Progression;
using UnityEngine;

namespace IdleClinic.ProgressionView
{
    /// <summary>Scene entry point: owns the rules, steps them with the frame time and draws the HUD.</summary>
    public sealed class ProgressionStage : MonoBehaviour
    {
        private static readonly int[] Speeds = { 1, 4, 16, 64 };
        private ClinicProgression game;
        private ProgressionWorld world;
        private Camera view;
        private int speedIndex;
        private readonly List<string> log = new List<string>();
        private GUIStyle label, chip, headline, small, onDark;

        private void Start()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            game = new ClinicProgression(new ProgressionSettings());
            game.Occurred += Log;
            world = new ProgressionWorld(game);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(52, -28, 0);
            RenderSettings.ambientLight = new Color(.62f, .62f, .66f);

            view = new GameObject("Camera").AddComponent<Camera>();
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.91f, .92f, .85f);
            view.orthographic = true;
            view.transform.rotation = Quaternion.Euler(58, 0, 0);
            gameObject.AddComponent<AudioListener>();
            FitCamera();
        }

        private void FitCamera()
        {
            var bounds = world.Bounds;
            view.transform.position = bounds.center - view.transform.forward * 60f;
            // Fit all eight corners of the play area, leaving a little more room for the HUD above and below.
            float reachX = 0, reachY = 0;
            for (var i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1) * 2 - Vector3.one) + Vector3.up * 1.5f;
                var local = view.transform.InverseTransformPoint(corner);
                reachX = Mathf.Max(reachX, Mathf.Abs(local.x)); reachY = Mathf.Max(reachY, Mathf.Abs(local.y));
            }
            view.orthographicSize = Mathf.Max(reachY * 1.22f, reachX / view.aspect * 1.08f);
            view.transform.position += view.transform.up * (view.orthographicSize * 0.04f);
        }

        private void Update()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight) { lastWidth = Screen.width; lastHeight = Screen.height; FitCamera(); }
            game.Tick(Time.deltaTime * Speeds[speedIndex]);
            world.Refresh();
        }

        private int lastWidth, lastHeight;

        private void Log(ProgressionEvent e)
        {
            string line = null;
            switch (e.Kind)
            {
                case ProgressionEventKind.RoomUnlocked: line = RoomCatalog.Definition(e.Room).Name + " unlocked"; break;
                case ProgressionEventKind.TaskCompleted: line = Describe(e.Task, true); break;
            }
            if (line == null) return;
            log.Add(line);
            if (log.Count > 4) log.RemoveAt(0);
        }

        private static string Describe(BossTask task, bool done)
        {
            var room = RoomCatalog.Definition(task.Room).Name;
            switch (task.Kind)
            {
                case BossTaskKind.Clean: return (done ? "Cleaned " : "Cleaning ") + room;
                case BossTaskKind.LevelUp: return (done ? "Built " : "Building ") + room;
                case BossTaskKind.UpgradeFurniture: return (done ? "Upgraded " : "Upgrading ") + RoomCatalog.Definition(task.Room).Furniture[task.FurnitureIndex].ToLowerInvariant() + " in " + room;
                default: return (done ? "Hired staff for " : "Hiring staff for ") + room;
            }
        }

        private string Status()
        {
            if (game.BossTravelling && game.BossDestination != null) return "Walking to " + RoomCatalog.Definition(game.BossDestination.Value).Name;
            var task = game.BossTask;
            if (task != null) return Describe(task.Value, false) + " " + Mathf.RoundToInt((float)game.BossProgress * 100) + "%";
            var next = game.NextTask;
            if (next == null) return "Nothing to do";
            return "Saving " + Coins(game.CostOf(next.Value)) + " for: " + Describe(next.Value, false).ToLowerInvariant();
        }

        private static string Coins(long value) => value >= 1000000 ? (value / 1000000d).ToString("0.#", CultureInfo.InvariantCulture) + "M"
            : value >= 10000 ? (value / 1000d).ToString("0.#", CultureInfo.InvariantCulture) + "k"
            : value.ToString("N0", CultureInfo.InvariantCulture);

        private void OnGUI()
        {
            if (game == null) return;
            var scale = Mathf.Max(0.8f, Mathf.Min(Screen.width, Screen.height) / 430f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            var width = Screen.width / scale; var height = Screen.height / scale;
            var safe = Screen.safeArea; var top = (Screen.height - safe.yMax) / scale + 6f; var bottom = safe.yMin / scale + 6f;
            Styles();

            GUI.Box(new Rect(8, top, width - 16, 74), GUIContent.none);
            GUI.Label(new Rect(18, top + 4, width - 36, 30), "Coins " + Coins(game.State.Wallet), headline);
            GUI.Label(new Rect(18, top + 32, width - 36, 20), Status(), label);
            GUI.Label(new Rect(18, top + 52, width - 36, 20), "Upgrades " + game.State.UpgradesPurchased + "   Next costs " + Coins(game.NextUpgradeCost) + "   Waiting " + game.State.PatientsWaiting, onDark);

            for (var i = 0; i < Speeds.Length; i++)
                if (GUI.Toggle(new Rect(8 + i * 62, height - bottom - 34, 58, 30), speedIndex == i, Speeds[i] + "x", chip)) speedIndex = i;
            for (var i = 0; i < log.Count; i++)
                GUI.Label(new Rect(8, height - bottom - 44 - (log.Count - i) * 18, width - 16, 18), log[i], small);

            foreach (var room in world.Rooms) DrawRoomLabel(room, scale);
        }

        private void DrawRoomLabel(RoomView room, float scale)
        {
            var screen = view.WorldToScreenPoint(room.LabelSpot);
            var state = game.State.Room(room.Id);
            var unlocked = game.State.IsUnlocked(room.Id);
            var text = RoomCatalog.Definition(room.Id).Name + (unlocked ? "  Lv " + state.Level : "  Locked");
            if (!unlocked) text += "\n" + Hint(room.Id);
            else if (state.Level > 0)
            {
                var furniture = 0; foreach (var f in state.FurnitureLevels) furniture += f;
                text += "\nfurniture " + furniture + "/" + state.FurnitureLevels.Length * state.Level;
            }
            var rect = new Rect(screen.x / scale - 70, (Screen.height - screen.y) / scale - 16, 140, 34);
            GUI.Label(rect, text, small);
        }

        private static string Hint(RoomId id)
        {
            switch (id)
            {
                case RoomId.Waiting: return "3+ patients waiting";
                default: return "Office level 2";
            }
        }

        private void Styles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            headline = new GUIStyle(label) { fontSize = 22 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.1f, .12f, .15f) } };
            onDark = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(.85f, .9f, .85f) } };
            chip = new GUIStyle(GUI.skin.button) { fontSize = 14 };
        }
    }
}
