using System;
using System.Collections.Generic;

namespace IdleClinic.Progression
{
    public interface IUnlockRule
    {
        bool IsMet(ProgressionState state);
    }

    public sealed class UnlockRule : IUnlockRule
    {
        private readonly Func<ProgressionState, bool> test;
        private UnlockRule(Func<ProgressionState, bool> test) { this.test = test; }
        public bool IsMet(ProgressionState state) => test(state);

        public static readonly IUnlockRule Always = new UnlockRule(_ => true);
        public static IUnlockRule PatientsWaitingAbove(int count) => new UnlockRule(s => s.PatientsWaiting > count);
        public static IUnlockRule OfficeAtLevel(int level) => new UnlockRule(s => s.Room(RoomId.Office).Level >= level);
    }

    /// <summary>Opens rooms whose rule is met. Unlocking is permanent.</summary>
    public static class UnlockRules
    {
        public static List<RoomId> Apply(ProgressionState state)
        {
            var opened = new List<RoomId>();
            foreach (var definition in RoomCatalog.All)
            {
                if (state.IsUnlocked(definition.Id) || !definition.Unlock.IsMet(state)) continue;
                state.Unlock(definition.Id);
                opened.Add(definition.Id);
            }
            return opened;
        }
    }
}
