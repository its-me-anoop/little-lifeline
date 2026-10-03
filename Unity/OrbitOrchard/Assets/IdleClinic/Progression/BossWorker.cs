using System;

namespace IdleClinic.Progression
{
    /// <summary>The boss: one task at a time, paid for when it starts, applied when it finishes.</summary>
    public sealed class BossWorker
    {
        private readonly ProgressionState state;
        private readonly IBossPlanner planner;
        private readonly ICostPolicy costs;
        private readonly ProgressionSettings settings;
        private double remaining;
        private BossTask? pending;

        public event Action<ProgressionEvent> Occurred;

        public BossTask? Current { get; private set; }
        public RoomId Location { get; private set; } = RoomId.Office;
        public RoomId? Destination { get; private set; }
        public bool IsTravelling => Destination != null;
        public double TravelProgress => IsTravelling && settings.TravelSeconds > 0 ? 1 - remaining / settings.TravelSeconds : 0;
        public double Progress { get; private set; }

        public BossWorker(ProgressionState state, IBossPlanner planner, ICostPolicy costs, ProgressionSettings settings)
        {
            this.state = state; this.planner = planner; this.costs = costs; this.settings = settings;
        }

        public void CaptureInto(BossSnapshot snapshot)
        {
            snapshot.Location = (int)Location; snapshot.Remaining = remaining;
            var task = Current ?? (IsTravelling ? pending : null);
            snapshot.HasTask = task != null;
            if (task != null) { snapshot.TaskKind = (int)task.Value.Kind; snapshot.TaskRoom = (int)task.Value.Room; snapshot.TaskFurniture = task.Value.FurnitureIndex; }
            snapshot.Travelling = IsTravelling;
            snapshot.Destination = (int)(Destination ?? RoomId.Office);
        }

        public void RestoreFrom(BossSnapshot snapshot)
        {
            Location = (RoomId)snapshot.Location; remaining = snapshot.Remaining;
            Current = null; pending = null; Destination = null; Progress = 0;
            if (!snapshot.HasTask) return;
            var task = new BossTask { Kind = (BossTaskKind)snapshot.TaskKind, Room = (RoomId)snapshot.TaskRoom, FurnitureIndex = snapshot.TaskFurniture };
            if (snapshot.Travelling) { pending = task; Destination = (RoomId)snapshot.Destination; return; }
            Current = task;
            var duration = Duration(task);
            Progress = duration <= 0 ? 1 : 1 - remaining / duration;
        }

        public void Tick(double seconds)
        {
            while (seconds > 0)
            {
                if (Current == null && !IsTravelling && !TryBegin()) return;
                var step = Math.Min(seconds, remaining);
                remaining -= step; seconds -= step;
                if (IsTravelling)
                {
                    if (remaining > 1e-9) return;
                    Location = Destination.Value; Destination = null;
                    var arrived = pending.Value; pending = null;
                    Start(arrived);
                    continue;
                }
                var duration = Duration(Current.Value);
                Progress = duration <= 0 ? 1 : 1 - remaining / duration;
                if (remaining > 1e-9) return;
                Finish();
            }
        }

        private bool TryBegin()
        {
            // Re-planned whenever the boss is free, so a newly opened room can jump the queue.
            var next = planner.Next(state);
            if (next == null) return false;
            var task = next.Value;
            var cost = CostOf(task);
            if (cost > state.Wallet) return false;
            if (task.Room == Location || settings.TravelSeconds <= 0) { Location = task.Room; Start(task); return true; }
            pending = task; Destination = task.Room; remaining = settings.TravelSeconds;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.BossTravelStarted, Task = task, Room = task.Room });
            return true;
        }

        private void Start(BossTask task)
        {
            var cost = CostOf(task);
            state.Wallet -= cost;
            if (IsUpgrade(task)) state.UpgradesPurchased++;
            Current = task; remaining = Duration(task); Progress = 0;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.TaskStarted, Task = task, Room = task.Room, Cost = cost });
        }

        private void Finish()
        {
            var task = Current.Value;
            var room = state.Room(task.Room);
            switch (task.Kind)
            {
                case BossTaskKind.Clean: room.IsClean = true; break;
                case BossTaskKind.LevelUp: room.Level++; break;
                case BossTaskKind.UpgradeFurniture: room.FurnitureLevels[task.FurnitureIndex]++; break;
                case BossTaskKind.Hire: room.StaffHired = true; break;
            }
            Current = null; Progress = 0;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.TaskCompleted, Task = task, Room = task.Room });
            foreach (var opened in UnlockRules.Apply(state))
                Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.RoomUnlocked, Room = opened });
        }

        private static bool IsUpgrade(BossTask task) =>
            task.Kind == BossTaskKind.LevelUp || task.Kind == BossTaskKind.UpgradeFurniture;

        public long CostOf(BossTask task)
        {
            switch (task.Kind)
            {
                case BossTaskKind.Clean: return 0;
                case BossTaskKind.Hire: return settings.HireCost;
                default: return costs.CostOfUpgrade(state.UpgradesPurchased);
            }
        }

        private double Duration(BossTask task)
        {
            switch (task.Kind)
            {
                case BossTaskKind.Clean: return settings.CleanSeconds;
                case BossTaskKind.LevelUp: return settings.BuildSeconds;
                case BossTaskKind.UpgradeFurniture: return settings.FurnitureSeconds;
                default: return settings.HireSeconds;
            }
        }
    }
}
