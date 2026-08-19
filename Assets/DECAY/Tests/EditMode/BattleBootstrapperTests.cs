using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class BattleBootstrapperTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        [Test]
        public void Create_AssemblesBattleInSetupWithBothRostersTrackedAndEnemyController()
        {
            DiceDefinition playerDefinition = Track(DiceTestFactory.CreateDefinition("dice.player_bootstrap"));
            DiceDefinition enemyDefinition = Track(DiceTestFactory.CreateDefinition("dice.enemy_bootstrap"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((11, playerDefinition), (12, playerDefinition));

            BattleRuntime runtime = new BattleBootstrapper().Create(
                config,
                global,
                new[] { new OwnedDiceId(11), new OwnedDiceId(12) },
                new[] { DiceRuntimeSeed.FromDefinition(enemyDefinition) },
                new SeededRandomSource(1),
                new SeededRandomSource(2));

            Assert.That(runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
            Assert.That(runtime.BattleInventoryState.TrackedCount(Side.Player), Is.EqualTo(2));
            Assert.That(runtime.BattleInventoryState.TrackedCount(Side.Enemy), Is.EqualTo(1));
            Assert.That(runtime.BattleInventoryState.TotalTrackedCount, Is.EqualTo(3));
            Assert.That(runtime.EnemyController, Is.Not.Null);
            Assert.That(runtime.BoardState.BrokenSlotCount(Side.Player), Is.Zero);
            Assert.That(runtime.History.Count, Is.Zero);
        }

        [Test]
        public void Create_PlayerDiceAreIndependentBattleCopiesWithPermanentSourceIdentity()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition("dice.player_copy", generalScoreValue: 3));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((77, definition));

            BattleRuntime runtime = CreateRuntime(config, global, new[] { new OwnedDiceId(77) });
            DiceRuntimeState battleDice = runtime.BattleInventoryState.GetDice(runtime.BattleInventoryState.TrackedDiceIds[0]);

            Assert.That(battleDice.Owner, Is.EqualTo(Side.Player));
            Assert.That(battleDice.SourceOwnedDiceId, Is.EqualTo(new OwnedDiceId(77)));
            Assert.That(battleDice.DefinitionId, Is.EqualTo(definition.Id));
            Assert.That(battleDice.GeneralScoreValue, Is.EqualTo(3));

            battleDice.SetGeneralScoreValue(99);
            Assert.That(global.GetDice(new OwnedDiceId(77)).BattleSeed.GeneralScoreValue, Is.EqualTo(3));
        }

        [Test]
        public void Create_AssignsUniqueBattleInstanceIdsWithoutUsingOwnedIdentityAsRuntimeIdentity()
        {
            DiceDefinition playerDefinition = Track(DiceTestFactory.CreateDefinition("dice.ids_player"));
            DiceDefinition enemyDefinition = Track(DiceTestFactory.CreateDefinition("dice.ids_enemy"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((5001, playerDefinition), (9002, playerDefinition));

            BattleRuntime runtime = new BattleBootstrapper().Create(
                config,
                global,
                new[] { new OwnedDiceId(5001), new OwnedDiceId(9002) },
                new[] { DiceRuntimeSeed.FromDefinition(enemyDefinition), DiceRuntimeSeed.FromDefinition(enemyDefinition) },
                new SeededRandomSource(10),
                new SeededRandomSource(11));

            Assert.That(runtime.BattleInventoryState.TrackedDiceIds, Is.EqualTo(new[]
            {
                new DiceInstanceId(1),
                new DiceInstanceId(2),
                new DiceInstanceId(3),
                new DiceInstanceId(4)
            }));
            Assert.That(runtime.BattleInventoryState.GetDice(new DiceInstanceId(1)).SourceOwnedDiceId, Is.EqualTo(new OwnedDiceId(5001)));
            Assert.That(runtime.BattleInventoryState.GetDice(new DiceInstanceId(3)).Owner, Is.EqualTo(Side.Enemy));
        }

        [Test]
        public void Create_RejectsDuplicatePlayerOwnedDiceSelectionBeforeRuntimeConstruction()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition("dice.duplicate_selection"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((1, definition));

            Assert.Throws<ArgumentException>(() => CreateRuntime(
                config,
                global,
                new[] { new OwnedDiceId(1), new OwnedDiceId(1) }));
        }

        [Test]
        public void Create_RejectsPlayerSelectionMissingFromGlobalInventory()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition("dice.missing_global"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((1, definition));

            Assert.Throws<ArgumentException>(() => CreateRuntime(
                config,
                global,
                new[] { new OwnedDiceId(2) }));
        }

        [Test]
        public void Create_RejectsNullEnemySeed()
        {
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = new GlobalInventoryState(Array.Empty<GlobalDiceState>());

            Assert.Throws<ArgumentException>(() => new BattleBootstrapper().Create(
                config,
                global,
                Array.Empty<OwnedDiceId>(),
                new DiceRuntimeSeed[] { null },
                new SeededRandomSource(1),
                new SeededRandomSource(2)));
        }

        [Test]
        public void Create_RequiresBothPrimaryAndFallbackRandomSources()
        {
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = new GlobalInventoryState(Array.Empty<GlobalDiceState>());
            var bootstrapper = new BattleBootstrapper();

            Assert.Throws<ArgumentNullException>(() => bootstrapper.Create(
                config, global, Array.Empty<OwnedDiceId>(), Array.Empty<DiceRuntimeSeed>(), null, new SeededRandomSource(2)));
            Assert.Throws<ArgumentNullException>(() => bootstrapper.Create(
                config, global, Array.Empty<OwnedDiceId>(), Array.Empty<DiceRuntimeSeed>(), new SeededRandomSource(1), null));
        }

        [Test]
        public void CreatedRuntime_UsesExistingMovementAuthorityRatherThanBootstrapMutationPath()
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition("dice.bootstrap_move"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            GlobalInventoryState global = CreateGlobal((1, definition));
            BattleRuntime runtime = CreateRuntime(config, global, new[] { new OwnedDiceId(1) });
            DiceInstanceId playerDiceId = runtime.BattleInventoryState.TrackedDiceIds[0];

            MoveDiceResult result = runtime.MoveDiceController.RequestMove(new MoveDiceRequest(
                Side.Player,
                playerDiceId,
                MoveDiceTarget.Board(new SlotId(Side.Player, 2))));

            Assert.That(result.IsApproved, Is.True);
            Assert.That(runtime.BoardState.GetSlot(new SlotId(Side.Player, 2)).OccupantDiceId, Is.EqualTo(playerDiceId));
            Assert.That(runtime.BattleInventoryState.IsInInventory(playerDiceId), Is.False);
            Assert.That(runtime.History.Facts[runtime.History.Count - 1], Is.SameAs(result.Fact));
        }

        [Test]
        public void TrackedDiceIds_IsReadOnlyAndOwnedByBattleInventoryState()
        {
            DiceRuntimeState dice = DiceTestFactory.CreateEnemyRuntimeDice(1);
            var inventory = new BattleInventoryState(10, new[] { dice });

            Assert.That(inventory.TrackedDiceIds, Is.Not.InstanceOf<List<DiceInstanceId>>());
            Assert.That(inventory.TrackedDiceIds, Is.EqualTo(new[] { new DiceInstanceId(1) }));
        }

        private BattleRuntime CreateRuntime(
            BattleConfig config,
            GlobalInventoryState global,
            IEnumerable<OwnedDiceId> selectedPlayerDiceIds)
        {
            return new BattleBootstrapper().Create(
                config,
                global,
                selectedPlayerDiceIds,
                Array.Empty<DiceRuntimeSeed>(),
                new SeededRandomSource(1),
                new SeededRandomSource(2));
        }

        private GlobalInventoryState CreateGlobal(params (long OwnedId, DiceDefinition Definition)[] entries)
        {
            var dice = new List<GlobalDiceState>();
            for (int i = 0; i < entries.Length; i++)
            {
                dice.Add(new GlobalDiceState(
                    new OwnedDiceId(entries[i].OwnedId),
                    DiceRuntimeSeed.FromDefinition(entries[i].Definition)));
            }
            return new GlobalInventoryState(dice);
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            _createdObjects.Add(value);
            return value;
        }
    }
}
