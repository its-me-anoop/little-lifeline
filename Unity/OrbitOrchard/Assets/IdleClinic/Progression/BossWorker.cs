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

        public event Action<ProgressionEvent> Occurred;

        public BossTask? Current { get; private set; }
        public double Progress { get; private set; }

        public BossWorker(ProgressionState state, IBossPlanner planner, ICostPolicy costs, ProgressionSettings settings)
        {
            this.state = state; this.planner = planner; this.costs = costs; this.settings = settings;
        }

        public void Tick(double seconds)
        {
            while (seconds > 0)
            {
                if (Current == null && !TryStart()) return;
                var step = Math.Min(seconds, remaining);
                remaining -= step; seconds -= step;
                var duration = Duration(Current.Value);
                Progress = duration <= 0 ? 1 : 1 - remaining / duration;
                if (remaining > 1e-9) return;
                Finish();
            }
        }

        private bool TryStart()
        {
            // Re-planned whenever the boss is free, so a newly opened room can jump the queue.
            var next = planner.Next(state);
            if (next == null) return false;
            var task = next.Value;
            var cost = CostOf(task);
            if (cost > state.Wallet) return false;
            state.Wallet -= cost;
            if (IsUpgrade(task)) state.UpgradesPurchased++;
            Current = task; remaining = Duration(task); Progress = 0;
            Occurred?.Invoke(new ProgressionEvent { Kind = ProgressionEventKind.TaskStarted, Task = task, Room = task.Room, Cost = cost });
            return true;
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

        private long CostOf(BossTask task)
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
