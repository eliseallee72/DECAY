using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class InventoryStateTests
    {
        [Test]
        public void GlobalInventory_ResolvesPermanentDiceByOwnedIdentity()
        {
            DiceDefinition definition = DiceTestFactory.CreateDefinition("dice.clockwork");
            DiceRuntimeSeed seed = DiceRuntimeSeed.FromDefinition(definition);
            var ownedDiceId = new OwnedDiceId(301);
            var globalDice = new GlobalDiceState(ownedDiceId, seed);
            var globalInventory = new GlobalInventoryState(new[] { globalDice });

            Assert.That(globalInventory.Count, Is.EqualTo(1));
            Assert.That(globalInventory.Contains(ownedDiceId), Is.True);
            Assert.That(globalInventory.GetDice(ownedDiceId), Is.SameAs(globalDice));
            Assert.That(globalInventory.GetDice(ownedDiceId).DefinitionId, Is.EqualTo(new DiceId("dice.clockwork")));
            Assert.That(globalInventory.GetDice(ownedDiceId).BattleSeed, Is.SameAs(seed));
        }

        [Test]
        public void GlobalInventory_ExposesReadOnlyPermanentCollections()
        {
            DiceRuntimeSeed seed = DiceRuntimeSeed.FromDefinition(DiceTestFactory.CreateDefinition("dice.read_only"));
            var ownedDiceId = new OwnedDiceId(309);
            var globalInventory = new GlobalInventoryState(new[] { new GlobalDiceState(ownedDiceId, seed) });

            var ownedIds = globalInventory.OwnedDiceIds as IList<OwnedDiceId>;
            var faces = seed.Faces as IList<DiceFaceSeed>;

            Assert.That(ownedIds, Is.Not.Null);
            Assert.That(faces, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => ownedIds.Add(new OwnedDiceId(310)));
            Assert.Throws<NotSupportedException>(() => faces.RemoveAt(0));
            Assert.That(globalInventory.Count, Is.EqualTo(1));
            Assert.That(seed.Faces.Count, Is.EqualTo(6));
        }

        [Test]
        public void GlobalInventory_RejectsDuplicateOwnedDiceIdentity()
        {
            DiceRuntimeSeed firstSeed = DiceRuntimeSeed.FromDefinition(DiceTestFactory.CreateDefinition("dice.first"));
            DiceRuntimeSeed secondSeed = DiceRuntimeSeed.FromDefinition(DiceTestFactory.CreateDefinition("dice.second"));
            var ownedDiceId = new OwnedDiceId(302);

            Assert.Throws<ArgumentException>(() => new GlobalInventoryState(new[]
            {
                new GlobalDiceState(ownedDiceId, firstSeed),
                new GlobalDiceState(ownedDiceId, secondSeed)
            }));
        }

        [Test]
        public void GlobalInventorySeed_CreatesIndependentBattleRuntimeCopy()
        {
            DiceRuntimeSeed globalSeed = DiceRuntimeSeed.FromDefinition(DiceTestFactory.CreateDefinition("dice.blessed", generalScoreValue: 2));
            var ownedDiceId = new OwnedDiceId(303);
            var globalInventory = new GlobalInventoryState(new[] { new GlobalDiceState(ownedDiceId, globalSeed) });

            DiceRuntimeState battleDice = DiceRuntimeState.CreatePlayerDice(
                new DiceInstanceId(401),
                ownedDiceId,
                globalInventory.GetDice(ownedDiceId).BattleSeed);
            battleDice.SetGeneralScoreValue(11);

            Assert.That(battleDice.GeneralScoreValue, Is.EqualTo(11));
            Assert.That(globalInventory.GetDice(ownedDiceId).BattleSeed.GeneralScoreValue, Is.EqualTo(2));
        }

        [Test]
        public void BattleInventory_TracksBothSidesSeparatelyAndStartsDiceInInventory()
        {
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(402, 304);
            DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(403);
            var battleInventory = new BattleInventoryState(10, new[] { player, enemy });

            Assert.That(battleInventory.TotalTrackedCount, Is.EqualTo(2));
            Assert.That(battleInventory.TrackedCount(Side.Player), Is.EqualTo(1));
            Assert.That(battleInventory.TrackedCount(Side.Enemy), Is.EqualTo(1));
            Assert.That(battleInventory.InventoryCount(Side.Player), Is.EqualTo(1));
            Assert.That(battleInventory.InventoryCount(Side.Enemy), Is.EqualTo(1));
            Assert.That(battleInventory.IsInInventory(player.InstanceId), Is.True);
            Assert.That(battleInventory.IsInInventory(enemy.InstanceId), Is.True);
        }

        [Test]
        public void BattleInventory_ExposesReadOnlyMembershipViews()
        {
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(409, 311);
            var battleInventory = new BattleInventoryState(10, new[] { player });
            var playerIds = battleInventory.InventoryDiceIds(Side.Player) as IList<DiceInstanceId>;

            Assert.That(playerIds, Is.Not.Null);
            Assert.Throws<NotSupportedException>(() => playerIds.Add(new DiceInstanceId(999)));
            Assert.That(battleInventory.InventoryCount(Side.Player), Is.EqualTo(1));
            Assert.That(battleInventory.IsInInventory(player.InstanceId), Is.True);
        }

        [Test]
        public void BattleInventory_RejectsDuplicateRuntimeDiceIdentity()
        {
            DiceRuntimeState first = DiceTestFactory.CreatePlayerRuntimeDice(404, 305, "dice.first");
            DiceRuntimeState second = DiceTestFactory.CreatePlayerRuntimeDice(404, 306, "dice.second");

            Assert.Throws<ArgumentException>(() => new BattleInventoryState(10, new[] { first, second }));
        }

        [Test]
        public void BattleInventory_EnforcesConfiguredCapacityPerSideAcrossEntireBattleRoster()
        {
            var dice = new List<DiceRuntimeState>();
            for (int i = 0; i < 11; i++)
            {
                dice.Add(DiceTestFactory.CreatePlayerRuntimeDice(500 + i, 600 + i, $"dice.player_{i}"));
            }

            Assert.Throws<ArgumentException>(() => new BattleInventoryState(10, dice));
        }

        [Test]
        public void RemovingInventoryMembership_DoesNotDeleteBattleRuntimeState()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(407, 307);
            var battleInventory = new BattleInventoryState(10, new[] { dice });

            DiceRuntimeState removed = battleInventory.RemoveFromInventory(dice.InstanceId);

            Assert.That(removed, Is.SameAs(dice));
            Assert.That(battleInventory.ContainsDice(dice.InstanceId), Is.True);
            Assert.That(battleInventory.IsInInventory(dice.InstanceId), Is.False);
            Assert.That(battleInventory.GetDice(dice.InstanceId), Is.SameAs(dice));
        }

        [Test]
        public void DecayedDice_CanRemainTrackedOutsideCurrentInventoryAndCannotBePlacedAgainThisGame()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(408, 308);
            var battleInventory = new BattleInventoryState(10, new[] { dice });
            var board = new BoardState();

            // DecayResolver will own this combined operation later. This test only proves
            // BattleInventoryState can represent the intended resulting membership state.
            battleInventory.RemoveFromInventory(dice.InstanceId);
            dice.MarkDecayedForCurrentGame();

            BattleState battle = DiceTestFactory.CreateBattleState();
            var command = new PlaceDiceOnBoardCommand(
                battle,
                board,
                battleInventory,
                dice.InstanceId,
                new SlotId(Side.Player, 1));

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(battleInventory.ContainsDice(dice.InstanceId), Is.True);
            Assert.That(battleInventory.IsInInventory(dice.InstanceId), Is.False);
        }

        [Test]
        public void BattleInventory_RejectsDuplicatePermanentPlayerDiceIdentity()
        {
            DiceRuntimeState first = DiceTestFactory.CreatePlayerRuntimeDice(410, 312, "dice.first_owned_copy");
            DiceRuntimeState second = DiceTestFactory.CreatePlayerRuntimeDice(411, 312, "dice.second_owned_copy");

            Assert.Throws<ArgumentException>(() => new BattleInventoryState(10, new[] { first, second }));
        }

        [Test]
        public void BattleInventory_RejectsDecayedDiceAsInitialInventoryMember()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(412, 313);
            dice.MarkDecayedForCurrentGame();

            Assert.Throws<ArgumentException>(() => new BattleInventoryState(10, new[] { dice }));
        }

        [Test]
        public void BattleInventory_ReturnToInventoryRejectsDecayedDiceAndKeepsItOutsideMembership()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(413, 314);
            var battleInventory = new BattleInventoryState(10, new[] { dice });
            battleInventory.RemoveFromInventory(dice.InstanceId);
            dice.MarkDecayedForCurrentGame();

            Assert.Throws<InvalidOperationException>(() => battleInventory.ReturnToInventory(dice.InstanceId));
            Assert.That(battleInventory.ContainsDice(dice.InstanceId), Is.True);
            Assert.That(battleInventory.IsInInventory(dice.InstanceId), Is.False);
        }

    }
}
