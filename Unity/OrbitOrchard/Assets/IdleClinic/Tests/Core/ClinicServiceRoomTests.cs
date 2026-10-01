using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;

namespace IdleClinic.Tests
{
    /// <summary>The starter clinic's office, staff room and store: built in order, equipped like the other rooms, and each
    /// with its own effect on the clinic.</summary>
    public sealed class ClinicServiceRoomTests
    {
        private static ClinicSimulation Rich()
        {
            var game = ClinicSimulation.CreateNew();
            game.Advance(25); Assert.That(game.Collect(0).Success, Is.True); Assert.That(game.HireNurse().Success, Is.True);
            game.Advance(30); Assert.That(game.State.Tutorial, Is.EqualTo(ClinicTutorialStep.Complete));
            Assert.That(ClinicSimulation.TryGrantReward(game.State, 500000000000L), Is.True);
            return game;
        }
        private static void Finish(ClinicSimulation game) { foreach (var job in game.State.Construction.ToArray()) game.CompleteConstructionNow(job.Id); }
        private static void BuildAll(ClinicSimulation game)
        {
            game.State.WaitingRoomUnlocked = true;
            Assert.That(game.BuildWaitingRoom().Success, Is.True); Finish(game);
            foreach (var kind in ClinicRules.ServiceRooms) { Assert.That(game.BuildRoom(kind).Success, Is.True, kind.ToString()); Finish(game); }
        }
        private static void MaxOut(ClinicSimulation game, ClinicRoom kind)
        {
            game.State.Room(kind).Tier = ClinicRules.MaximumTier(game.State);
            for (var item = 0; item < ClinicGear.ItemCount; item++)
                while (!ClinicGear.AtTop(game.State, kind, item)) Assert.That(game.UpgradeGear(kind, item).Success, Is.True);
        }

        [Test] public void NewStarterClinicsHaveTheThreeBackRoomsAsUnbuiltPlots()
        {
            var game = ClinicSimulation.CreateNew();
            foreach (var kind in ClinicRules.ServiceRooms)
            {
                var room = game.State.Room(kind);
                Assert.That(room, Is.Not.Null, kind.ToString()); Assert.That(room.Built, Is.False);
                Assert.That(ClinicGear.Active(game.State, kind), Is.False);
            }
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
            Assert.That(ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic).State.Rooms.Any(r => ClinicRules.IsServiceRoom(r.Kind)), Is.False);
        }

        [Test] public void BackRoomsAreBuiltInOrderAfterTheWaitingRoom()
        {
            var game = Rich();
            Assert.That(game.BuildRoom(ClinicRoom.Office).Success, Is.False, "The waiting room comes first.");
            game.State.WaitingRoomUnlocked = true; Assert.That(game.BuildWaitingRoom().Success, Is.True); Finish(game);
            Assert.That(game.BuildRoom(ClinicRoom.StaffRoom).Success, Is.False, "The office comes before the staff room.");
            var wallet = game.State.Wallet;
            Assert.That(game.BuildRoom(ClinicRoom.Office).Success, Is.True);
            Assert.That(wallet - game.State.Wallet, Is.EqualTo(ClinicRules.RoomBuildCost(game.State, ClinicRoom.Office)));
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True, "A save taken mid-build is valid.");
            Assert.That(game.BuildRoom(ClinicRoom.Office).Success, Is.False, "Already being built.");
            game.Advance(ClinicRules.RoomBuildSeconds(game.State, ClinicRoom.Office) + 1);
            Assert.That(game.State.Room(ClinicRoom.Office).Built, Is.True);
            Assert.That(game.BuildRoom(ClinicRoom.StaffRoom).Success, Is.True); Finish(game);
            Assert.That(game.BuildRoom(ClinicRoom.Store).Success, Is.True); Finish(game);
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
            Assert.That(game.Renovate(ClinicRoom.Store).Success, Is.True, "Built back rooms renovate like any other.");
        }

        [Test] public void OfficeEquipmentRaisesTheVisitFee()
        {
            var game = Rich(); BuildAll(game);
            var before = ClinicRules.VisitFee(game.State);
            Assert.That(game.UpgradeGear(ClinicRoom.Office, 0).Success, Is.True);
            Assert.That(ClinicRules.OfficeFeePercent(game.State), Is.GreaterThanOrEqualTo(0));
            MaxOut(game, ClinicRoom.Office);
            Assert.That(ClinicRules.OfficeFeePercent(game.State), Is.EqualTo(100));
            Assert.That(ClinicRules.VisitFee(game.State), Is.EqualTo(before * 2).Within(1));
            Assert.That(ClinicRules.VisitFee(game.State), Is.LessThanOrEqualTo(ClinicRules.MaximumVisitFee(game.State)));
            game.Advance(120);
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True, "Clinics earning the office's higher fee still validate.");
        }

        [Test] public void StaffRoomEquipmentMakesEveryServiceQuicker()
        {
            var game = Rich(); BuildAll(game);
            var checkIn = ClinicRules.ReceptionTicks(game.State); var care = ClinicRules.TreatmentTicks(game.State);
            MaxOut(game, ClinicRoom.StaffRoom);
            Assert.That(ClinicRules.StaffRoomSpeedPercent(game.State), Is.EqualTo(30));
            Assert.That(ClinicRules.ReceptionTicks(game.State), Is.LessThan(checkIn));
            Assert.That(ClinicRules.TreatmentTicks(game.State), Is.LessThan(care));
            Assert.That(ClinicRules.TreatmentTicks(game.State), Is.GreaterThanOrEqualTo(ClinicRules.FastestTreatment(game.State)));
        }

        [Test] public void StoreEquipmentAddsQueuePlacesAndCorridorSeats()
        {
            var game = Rich(); BuildAll(game);
            var queue = ClinicRules.UnpaidQueueCapacity(game.State); var seats = ClinicRules.WaitingCapacity(game.State);
            MaxOut(game, ClinicRoom.Store);
            Assert.That(ClinicRules.UnpaidQueueCapacity(game.State), Is.EqualTo(queue + 3));
            Assert.That(ClinicRules.WaitingCapacity(game.State), Is.EqualTo(seats + 3));
            foreach (var room in game.State.Rooms.Where(r => !ClinicRules.IsServiceRoom(r.Kind))) { room.Tier = 20; room.FacilitiesLevel = 10; }
            Assert.That(ClinicRules.UnpaidQueueCapacity(game.State), Is.EqualTo(ClinicRules.StarterQueuePlaces), "Every queue place has a place to stand.");
            Assert.That(ClinicRules.WaitingCapacity(game.State), Is.EqualTo(ClinicRules.StarterSeats), "Every seat has a chair.");
            game.Advance(300);
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
        }

        [Test] public void OlderRulesKeepThreeRoomsAndMoveToTheBackRoomsWhenTheyAdopt()
        {
            var game = ClinicSimulation.CreateNew();
            var state = game.State;
            state.Rooms.RemoveAll(r => ClinicRules.IsServiceRoom(r.Kind));
            state.RulesVersion = 4;
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
            Assert.That(new ClinicSimulation(state).State.Rooms.Count, Is.EqualTo(3), "Rules 4 offer no back rooms.");
            Assert.That(ClinicStateMigration.TryAdoptCurrentRules(state), Is.True);
            Assert.That(new ClinicSimulation(state).State.Rooms.Count, Is.EqualTo(6));
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
        }
    }
}
