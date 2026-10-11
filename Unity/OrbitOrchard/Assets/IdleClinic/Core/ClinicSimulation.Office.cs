using System;
using System.Linq;

namespace IdleClinic.Core
{
    public sealed partial class ClinicSimulation
    {
        public bool OfficeAnchored => !ClinicRules.IsDoctors(State) && (State.OpeningEnabled || State.OfficeLevel > 0);
        public bool OfficeUpgrading => State.OfficeUpgradeEndsTick >= 0;
        public bool RoomsAtOfficeLevel => State.Rooms.All(r => r.Built && r.Tier >= State.OfficeLevel)
            && State.Construction.Count == 0;

        // Activate the anchor when an existing campaign is bound to the app. Never lower a
        // built room or cancel paid work from a save made before offices had progression.
        public void EnsureOfficeAnchor()
        {
            if (ClinicRules.IsDoctors(State) || State.OpeningEnabled || State.OfficeLevel > 0) return;
            State.OfficeLevel = Math.Max(1, Math.Max(State.Rooms.Max(r => r.Tier),
                State.Construction.Select(c => c.TargetTier).DefaultIfEmpty(1).Max()));
            State.OfficeCleaned = true;
        }

        public long OfficeUpgradeCost => OfficePrice(State);
        public int OfficeUpgradeSeconds => OfficeSeconds(State);
        private static long OfficePrice(ClinicState state) => ClinicRules.Compound(
            ClinicBalance.For(state).RenovationBase[(int)ClinicRoom.Reception],
            ClinicBalance.For(state).RenovationGrowth, state.OfficeLevel - 1);
        private static int OfficeSeconds(ClinicState state) => (int)Math.Min(ClinicBalance.For(state).MaximumConstructionSeconds,
            ClinicRules.Compound(ClinicBalance.For(state).RenovationBaseSeconds,
                ClinicBalance.For(state).RenovationTimeGrowth, state.OfficeLevel - 1));

        public ClinicCommandResult UpgradeOffice()
        {
            if (ClinicRules.IsDoctors(State) || !TutorialComplete()) return No("Finish opening the clinic first.");
            EnsureOfficeAnchor();
            if (OfficeUpgrading) return No("The office upgrade is already underway.");
            if (State.OfficeLevel >= ClinicRules.MaximumTier(State)) return No("The clinic has reached its highest level.");
            if (!RoomsAtOfficeLevel) return No("Bring reception, waiting and nursing to clinic level " + State.OfficeLevel + " first.");
            if (BuildersBusy) return No(BuildersBusyMessage);
            var cost = OfficeUpgradeCost;
            if (!CanSpend(cost)) return No("Save " + cost + " coins for the office upgrade.");
            Spend(cost);
            State.OfficeUpgradeStartedTick = State.Tick;
            State.OfficeUpgradeEndsTick = State.Tick + OfficeUpgradeSeconds * 10L;
            Emit(ClinicEventKind.ConstructionStarted, ClinicRoom.Reception, source:"office");
            return Yes("Office upgrade started. The next clinic level unlocks when it finishes.", cost);
        }

        private void FinishOfficeUpgrade()
        {
            if (!OfficeUpgrading || State.OfficeUpgradeEndsTick > State.Tick) return;
            State.OfficeLevel++;
            State.OfficeUpgradeEndsTick = -1;
            Emit(ClinicEventKind.ConstructionCompleted, ClinicRoom.Reception, source:"office");
        }

        private static bool ValidOffice(ClinicState state)
        {
            if (state.OfficeLevel < 0 || state.OfficeLevel > ClinicRules.MaximumTier(state)
                || state.OfficeUpgradeStartedTick < 0 || state.OfficeUpgradeStartedTick > state.Tick
                || state.OfficeUpgradeEndsTick < -1) return false;
            if (ClinicRules.IsDoctors(state)) return state.OfficeUpgradeEndsTick == -1;
            if (state.OfficeLevel > 0 && (state.Rooms.Any(r => r.Tier > state.OfficeLevel)
                || state.Construction.Any(c => c.TargetTier > state.OfficeLevel))) return false;
            if (state.OfficeUpgradeEndsTick == -1) return true;
            return state.Tutorial == ClinicTutorialStep.Complete && state.OfficeCleaned
                && state.OfficeLevel >= 1 && state.OfficeLevel < ClinicRules.MaximumTier(state)
                && state.OfficeUpgradeEndsTick > state.Tick
                && state.OfficeUpgradeEndsTick - state.OfficeUpgradeStartedTick == OfficeSeconds(state) * 10L
                && state.TotalSpent >= OfficePrice(state)
                && state.Rooms.All(r => r.Built && r.Tier == state.OfficeLevel)
                && state.Construction.Count == 0;
        }
    }
}
