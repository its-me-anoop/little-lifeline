using System;
using System.Collections.Generic;
using System.Linq;
using IdleClinic.Core;
using NUnit.Framework;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>Room equipment: twenty pieces that arrive with room size, ten versions each, priced and timed per piece.</summary>
    public sealed class ClinicGearTests
    {
        private static readonly ClinicRoom[] StarterRooms = { ClinicRoom.Reception, ClinicRoom.FirstAid };
        private static ClinicSimulation Rich(int tier = 1)
        {
            var game = ClinicSimulation.CreateNew();
            game.Advance(25); Assert.That(game.Collect(0).Success, Is.True); Assert.That(game.HireNurse().Success, Is.True);
            game.Advance(30); Assert.That(game.State.Tutorial, Is.EqualTo(ClinicTutorialStep.Complete));
            foreach (var room in game.State.Rooms.Where(r => r.Built)) room.Tier = tier;
            Assert.That(ClinicSimulation.TryGrantReward(game.State, 500000000000L), Is.True);
            return game;
        }

        [TestCase(ClinicRoom.Reception)]
        [TestCase(ClinicRoom.FirstAid)]
        public void NewClinicsHaveTwentyBasicPiecesAndOnlyTheFirstIsUnlocked(ClinicRoom room)
        {
            var game = ClinicSimulation.CreateNew();
            Assert.That(ClinicGear.Active(game.State, room), Is.True);
            Assert.That(Enumerable.Range(0, 20).All(item => ClinicGear.Version(game.State, room, item) == 1), Is.True);
            Assert.That(ClinicGear.UnlockedCount(game.State, room), Is.EqualTo(1));
            Assert.That(ClinicGear.Steps(game.State, room), Is.Zero);
            Assert.That(ClinicGear.Active(game.State, ClinicRoom.Waiting), Is.False, "An unbuilt room has no equipment.");
        }

        [Test] public void EachRoomSizeUnlocksExactlyOneMorePieceInTheStarterClinic()
        {
            var game = Rich();
            for (var tier = 1; tier <= 20; tier++)
            {
                game.State.Room(ClinicRoom.FirstAid).Tier = tier;
                Assert.That(ClinicGear.UnlockedCount(game.State, ClinicRoom.FirstAid), Is.EqualTo(tier));
            }
        }

        [Test] public void TheDoctorsClinicSpreadsTwentyPiecesOverFortyRoomSizes()
        {
            var state = ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic).State;
            var tiers = Enumerable.Range(0, 20).Select(item => ClinicGear.UnlockTier(state, item)).ToList();
            Assert.That(tiers[0], Is.EqualTo(1)); Assert.That(tiers[19], Is.EqualTo(40));
            Assert.That(tiers, Is.Ordered.Ascending.And.Unique);
            foreach (ClinicRoom room in Enum.GetValues(typeof(ClinicRoom))) Assert.That(ClinicGear.Active(state, room), Is.EqualTo(!ClinicRules.IsServiceRoom(room)), room.ToString());
        }

        [Test] public void LockedPiecesCannotBeUpgradedAndTheOldTracksAreRetired()
        {
            var game = Rich(3);
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, 3).Success, Is.False);
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, -1).Success, Is.False);
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, 20).Success, Is.False);
            Assert.That(game.UpgradeGear(ClinicRoom.Waiting, 0).Success, Is.False, "Not built.");
            Assert.That(game.Upgrade(ClinicRoom.FirstAid, UpgradeTrack.Equipment).Success, Is.False);
            Assert.That(game.Upgrade(ClinicRoom.Reception, UpgradeTrack.Facilities).Success, Is.False);
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, 2).Success, Is.True);
        }

        [Test] public void EveryUnlockedPieceTakesNineUpgradesToItsAdvancedVersion()
        {
            var game = Rich(1);
            foreach (var room in StarterRooms)
            {
                for (var version = 2; version <= 10; version++)
                {
                    Assert.That(game.UpgradeGear(room, 0).Success, Is.True, room + " to version " + version);
                    Assert.That(ClinicGear.Version(game.State, room, 0), Is.EqualTo(version));
                }
                Assert.That(game.UpgradeGear(room, 0).Success, Is.False, "Version 10 is the most advanced.");
                Assert.That(ClinicGear.UpgradeCost(game.State, room, 0), Is.Zero);
            }
        }

        [Test] public void PricesGrowExponentiallyWithinAPieceAndFromPieceToPiece()
        {
            var game = Rich(20);
            var previousFirst = 0L;
            for (var item = 0; item < 20; item++)
            {
                var costs = new List<long>();
                for (var version = 2; version <= 10; version++)
                {
                    costs.Add(ClinicGear.UpgradeCost(game.State, ClinicRoom.FirstAid, item));
                    Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, item).Success, Is.True);
                }
                for (var i = 1; i < costs.Count; i++) Assert.That(costs[i], Is.GreaterThan(costs[i - 1] * 13 / 10), "piece " + item + " step " + i);
                Assert.That(costs[0], Is.GreaterThan(previousFirst), "each piece starts dearer than the last");
                previousFirst = costs[0];
            }
            Assert.That(ClinicGear.UpgradeCost(ClinicSimulation.CreateNew().State, ClinicRoom.FirstAid, 0), Is.EqualTo(48));
            Assert.That(ClinicGear.UpgradeCost(ClinicSimulation.CreateNew().State, ClinicRoom.Reception, 0), Is.EqualTo(36));
        }

        [Test] public void EachUpgradeRemovesATenthOfTheTimeStillToLoseDownToAFloor()
        {
            var state = Rich(20).State;
            var floor = ClinicGear.FloorTicks(state, ClinicRoom.FirstAid);
            Assert.That(floor, Is.EqualTo((180 * 6 + 99) / 100));
            var ticks = Enumerable.Range(0, 181).Select(step => ClinicGear.Apply(state, ClinicRoom.FirstAid, 180, step)).ToList();
            Assert.That(ticks[0], Is.EqualTo(180));
            Assert.That(ticks, Is.Ordered.Descending);
            for (var step = 1; step <= 60; step++)
                Assert.That(ticks[step] - floor, Is.EqualTo((180 - floor) * Math.Pow(.9, step)).Within(1.01), "step " + step);
            Assert.That(ticks[180], Is.EqualTo(floor).Within(1));
            Assert.That(ticks[1], Is.LessThan(ticks[0] * 93 / 100), "The first upgrade is worth about eight per cent.");
        }

        [Test] public void UpgradingChargesThePriceSpeedsTheRoomAndKeepsCoarseLevelsInStep()
        {
            var game = Rich(2);
            var before = ClinicRules.TreatmentTicks(game.State);
            var price = ClinicGear.UpgradeCost(game.State, ClinicRoom.FirstAid, 1);
            var wallet = game.State.Wallet;
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, 1).Cost, Is.EqualTo(price));
            Assert.That(game.State.Wallet, Is.EqualTo(wallet - price));
            for (var i = 0; i < 17; i++) Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, i % 2).Success, Is.True, "upgrade " + i);
            var room = game.State.Room(ClinicRoom.FirstAid);
            Assert.That(ClinicGear.Steps(room), Is.EqualTo(18));
            Assert.That(room.EquipmentLevel, Is.EqualTo(1 + 18 * 9 / 180));
            Assert.That(room.FacilitiesLevel, Is.GreaterThanOrEqualTo(room.EquipmentLevel));
            Assert.That(ClinicRules.TreatmentTicks(game.State), Is.LessThan(before));
            Assert.That(new ClinicSimulation(JsonUtility.FromJson<ClinicState>(JsonUtility.ToJson(game.State))).State.Room(ClinicRoom.FirstAid).GearLevels, Is.EqualTo(room.GearLevels));
            Assert.That(ClinicSimulation.IsValidState(game.State), Is.True);
        }

        [Test] public void EveryStarterRoomGetsFasterWithItsOwnEquipment()
        {
            var game = Rich(3);
            var reception = ClinicRules.ReceptionTicks(game.State); var care = ClinicRules.TreatmentTicks(game.State);
            Assert.That(game.UpgradeGear(ClinicRoom.Reception, 0).Success, Is.True);
            Assert.That(ClinicRules.ReceptionTicks(game.State), Is.LessThan(reception));
            Assert.That(ClinicRules.TreatmentTicks(game.State), Is.EqualTo(care), "Another room's equipment does not speed this one up.");
        }

        [Test] public void AClinicThatOwnedEquipmentLevelsKeepsAtLeastItsServiceSpeed()
        {
            for (var level = 1; level <= 10; level++)
            {
                var state = ClinicSimulation.CreateNew().State;
                state.Room(ClinicRoom.FirstAid).Tier = 20;
                state.Room(ClinicRoom.FirstAid).EquipmentLevel = level;
                var oldSpeed = 100 + 15 * (level - 1);
                var legacy = Math.Max(29, (18000 + oldSpeed - 1) / oldSpeed);
                var now = ClinicRules.TreatmentTicks(state);
                Assert.That(now, Is.LessThanOrEqualTo(legacy), "level " + level + ": never slower");
                Assert.That(now, Is.GreaterThanOrEqualTo(Math.Max(29, ClinicGear.FloorTicks(state, ClinicRoom.FirstAid))), "and never below the floor");
                var steps = ClinicGear.Steps(state, ClinicRoom.FirstAid);
                ClinicGear.EnsureSeeded(state, ClinicRoom.FirstAid);
                Assert.That(state.Room(ClinicRoom.FirstAid).GearLevels, Has.Count.EqualTo(20));
                Assert.That(ClinicGear.Steps(state, ClinicRoom.FirstAid), Is.EqualTo(steps), "converted, not lost");
                Assert.That(ClinicRules.TreatmentTicks(state), Is.EqualTo(now), "seeding changes no time");
                Assert.That(ClinicSimulation.IsValidState(state), Is.True);
            }
        }

        [Test] public void EarlierRulesKeepTheClassicTracks()
        {
            var game = ClinicSimulation.CreateNew(); game.State.RulesVersion = 4;
            Assert.That(ClinicGear.Active(game.State, ClinicRoom.FirstAid), Is.False);
            Assert.That(ClinicGear.UpgradeCost(game.State, ClinicRoom.FirstAid, 0), Is.Zero);
            Assert.That(game.UpgradeGear(ClinicRoom.FirstAid, 0).Success, Is.False);
        }

        [Test] public void TamperedPieceLevelsAreRejected()
        {
            var state = JsonUtility.FromJson<ClinicState>(JsonUtility.ToJson(ClinicSimulation.CreateNew().State));
            ClinicGear.EnsureSeeded(state, ClinicRoom.FirstAid);
            var room = state.Room(ClinicRoom.FirstAid);
            room.GearLevels[4] = 11;
            Assert.That(ClinicSimulation.IsValidState(state), Is.False);
            room.GearLevels[4] = 0;
            Assert.That(ClinicSimulation.IsValidState(state), Is.False);
            room.GearLevels[4] = 5; room.GearLevels.RemoveAt(19);
            Assert.That(ClinicSimulation.IsValidState(state), Is.False);
            room.GearLevels.Add(1);
            Assert.That(ClinicSimulation.IsValidState(state), Is.True);
        }

        [Test] public void EveryPieceAndVersionHasAName()
        {
            foreach (ClinicRoom room in Enum.GetValues(typeof(ClinicRoom)))
                Assert.That(Enumerable.Range(0, 20).Select(item => ClinicGear.ItemName(room, item)).Distinct().Count(), Is.EqualTo(20), room.ToString());
            Assert.That(Enumerable.Range(1, 10).Select(ClinicGear.VersionName).Distinct().Count(), Is.EqualTo(10));
        }
    }
}
