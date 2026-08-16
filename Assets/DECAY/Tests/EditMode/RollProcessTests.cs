using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Decay.Tests
{
    public sealed class RollProcessTests
    {
        [Test]
        public void SeededRandomSource_SameSeedProducesSameSequence()
        {
            var first = new SeededRandomSource(314159);
            var second = new SeededRandomSource(314159);

            for (int i = 0; i < 24; i++)
            {
                Assert.That(first.NextInt(1, 7), Is.EqualTo(second.NextInt(1, 7)));
            }
        }

        [Test]
        public void SeededRandomSource_RejectsInvalidRange()
        {
            var source = new SeededRandomSource(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => source.NextInt(4, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => source.NextInt(5, 2));
        }

        [Test]
        public void ScriptedRandomSource_ReturnsAuthoredValuesInOrder()
        {
            var source = new ScriptedRandomSource(new[] { 6, 1, 4 });

            Assert.That(source.NextInt(1, 7), Is.EqualTo(6));
            Assert.That(source.NextInt(1, 7), Is.EqualTo(1));
            Assert.That(source.NextInt(1, 7), Is.EqualTo(4));
            Assert.That(source.RemainingCount, Is.Zero);
        }

        [Test]
        public void ScriptedRandomSource_InvalidValueFailsWithoutConsumingIt()
        {
            var source = new ScriptedRandomSource(new[] { 7, 3 });

            Assert.Throws<RecoverableRandomSourceException>(() => source.NextInt(1, 7));
            Assert.That(source.RemainingCount, Is.EqualTo(2));
        }

        [Test]
        public void ScriptedRandomSource_ThrowsWhenSequenceIsExhausted()
        {
            var source = new ScriptedRandomSource(new[] { 2 });
            Assert.That(source.NextInt(1, 7), Is.EqualTo(2));

            Assert.Throws<RecoverableRandomSourceException>(() => source.NextInt(1, 7));
        }

        [Test]
        public void DiceRollResolver_SelectsFaceIndexNotRollValue()
        {
            DiceDefinition definition = DiceTestFactory.CreateDefinition(
                rollValues: new[] { 6, 1, 4 },
                faceScoreValues: new[] { 20, 30, 40 });
            try
            {
                DiceRuntimeState dice = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
                var resolver = new DiceRollResolver(new ScriptedRandomSource(new[] { 2 }));

                int faceIndex = resolver.ResolveFaceIndex(dice);

                Assert.That(faceIndex, Is.EqualTo(2));
                Assert.That(dice.TryGetFace(faceIndex, out DiceFaceRuntimeState face), Is.True);
                Assert.That(face.RollValue, Is.EqualTo(1));
                Assert.That(face.ScoreValue, Is.EqualTo(30));
                Assert.That(dice.HasCurrentFace, Is.False, "Resolving a roll must not mutate dice state.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void DiceRollResolver_UsesActualFaceCount()
        {
            DiceDefinition definition = DiceTestFactory.CreateDefinition(
                rollValues: new[] { 1, 3, 6 },
                faceScoreValues: new[] { 1, 3, 6 });
            try
            {
                DiceRuntimeState dice = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(1), definition);
                var resolver = new DiceRollResolver(new ScriptedRandomSource(new[] { 3 }));

                Assert.That(resolver.ResolveFaceIndex(dice), Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void RollExecutor_RequiresRollingPhaseWithoutMutatingStateOrHistory()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(1, 1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { dice });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, dice, new SlotId(Side.Player, 1));
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 4 }));

            Assert.Throws<InvalidOperationException>(() => executor.ExecuteRoll());
            Assert.That(dice.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_EmptyBoardProducesNoRollFacts()
        {
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, Array.Empty<DiceRuntimeState>());
            var history = new BattleHistory();
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(Array.Empty<int>()));

            IReadOnlyList<DiceRolledFact> facts = executor.ExecuteRoll().Facts;

            Assert.That(facts, Is.Empty);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_RollsOccupiedEnemyAndPlayerSlotsAndSkipsEmptySlots()
        {
            DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(1);
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(2, 2);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { enemy, player });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, enemy, new SlotId(Side.Enemy, 2));
            PlaceForTest(board, inventory, player, new SlotId(Side.Player, 5));
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 6, 3 }));

            IReadOnlyList<DiceRolledFact> facts = executor.ExecuteRoll().Facts;

            Assert.That(facts.Count, Is.EqualTo(2));
            Assert.That(enemy.CurrentFaceIndex, Is.EqualTo(6));
            Assert.That(player.CurrentFaceIndex, Is.EqualTo(3));
            Assert.That(history.Count, Is.EqualTo(2));
        }

        [Test]
        public void RollExecutor_ExhaustedScriptDoesNotLeavePartialRollState()
        {
            DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(1);
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(2, 2);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { enemy, player });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, enemy, new SlotId(Side.Enemy, 1));
            PlaceForTest(board, inventory, player, new SlotId(Side.Player, 1));
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 4 }));

            Assert.Throws<RecoverableRandomSourceException>(() => executor.ExecuteRoll());

            Assert.That(enemy.HasCurrentFace, Is.False);
            Assert.That(player.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_RecoverablePrimaryFailureRetriesEntirePlanWithFallbackBeforeCommit()
        {
            DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(1);
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(2, 2);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { enemy, player });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, enemy, new SlotId(Side.Enemy, 1));
            PlaceForTest(board, inventory, player, new SlotId(Side.Player, 1));
            var primary = new ScriptedRandomSource(new[] { 4 });
            var fallback = new ScriptedRandomSource(new[] { 2, 5 });
            var executor = new RollExecutor(battle, board, inventory, history, primary, fallback);

            RollExecutionResult result = executor.ExecuteRoll();

            Assert.That(result.UsedFallbackRandomSource, Is.True);
            Assert.That(enemy.CurrentFaceIndex, Is.EqualTo(2), "The partial primary plan must be discarded, not mixed with fallback.");
            Assert.That(player.CurrentFaceIndex, Is.EqualTo(5));
            Assert.That(result.Resolutions.Count, Is.EqualTo(2));
            Assert.That(history.Count, Is.EqualTo(2));
            Assert.That(fallback.RemainingCount, Is.Zero);
        }

        [Test]
        public void RollExecutor_UsesExplicitPairThenEnemyPlayerRandomDrawOrder()
        {
            DiceRuntimeState enemyOne = DiceTestFactory.CreateEnemyRuntimeDice(1, "dice.enemy_one");
            DiceRuntimeState playerOne = DiceTestFactory.CreatePlayerRuntimeDice(2, 2, "dice.player_one");
            DiceRuntimeState enemyTwo = DiceTestFactory.CreateEnemyRuntimeDice(3, "dice.enemy_two");
            DiceRuntimeState playerTwo = DiceTestFactory.CreatePlayerRuntimeDice(4, 4, "dice.player_two");
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { playerTwo, enemyTwo, playerOne, enemyOne });
            var history = new BattleHistory();

            PlaceForTest(board, inventory, playerTwo, new SlotId(Side.Player, 2));
            PlaceForTest(board, inventory, enemyTwo, new SlotId(Side.Enemy, 2));
            PlaceForTest(board, inventory, playerOne, new SlotId(Side.Player, 1));
            PlaceForTest(board, inventory, enemyOne, new SlotId(Side.Enemy, 1));

            var executor = new RollExecutor(
                battle,
                board,
                inventory,
                history,
                new ScriptedRandomSource(new[] { 1, 2, 3, 4 }));

            IReadOnlyList<DiceRolledFact> facts = executor.ExecuteRoll().Facts;

            Assert.That(facts[0].SlotId, Is.EqualTo(new SlotId(Side.Enemy, 1)));
            Assert.That(facts[0].FaceIndex, Is.EqualTo(1));
            Assert.That(facts[1].SlotId, Is.EqualTo(new SlotId(Side.Player, 1)));
            Assert.That(facts[1].FaceIndex, Is.EqualTo(2));
            Assert.That(facts[2].SlotId, Is.EqualTo(new SlotId(Side.Enemy, 2)));
            Assert.That(facts[2].FaceIndex, Is.EqualTo(3));
            Assert.That(facts[3].SlotId, Is.EqualTo(new SlotId(Side.Player, 2)));
            Assert.That(facts[3].FaceIndex, Is.EqualTo(4));
        }

        [Test]
        public void RollExecutor_FaceMutationChangesRollValueWithoutChangingSelectedFaceIdentity()
        {
            DiceDefinition definition = DiceTestFactory.CreateDefinition();
            try
            {
                DiceRuntimeState dice = DiceRuntimeState.CreatePlayerDice(
                    new DiceInstanceId(10),
                    new OwnedDiceId(10),
                    definition);
                Assert.That(dice.TryGetFace(5, out DiceFaceRuntimeState faceFive), Is.True);
                faceFive.SetRollValue(3);

                BattleState battle = DiceTestFactory.CreateBattleState();
                battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
                var board = new BoardState();
                var inventory = new BattleInventoryState(10, new[] { dice });
                var history = new BattleHistory();
                var slot = new SlotId(Side.Player, 3);
                PlaceForTest(board, inventory, dice, slot);
                var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 5 }));

                DiceRolledFact fact = executor.ExecuteRoll().Facts[0];

                Assert.That(dice.CurrentFaceIndex, Is.EqualTo(5));
                Assert.That(dice.ActiveRollValue, Is.EqualTo(3));
                Assert.That(fact.FaceIndex, Is.EqualTo(5));
                Assert.That(fact.RollValue, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void RollExecutor_OverwritesPreviousCurrentFace()
        {
            DiceRuntimeState dice = DiceTestFactory.CreateEnemyRuntimeDice(1);
            dice.SetCurrentFace(6);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { dice });
            PlaceForTest(board, inventory, dice, new SlotId(Side.Enemy, 4));
            var executor = new RollExecutor(
                battle,
                board,
                inventory,
                new BattleHistory(),
                new ScriptedRandomSource(new[] { 2 }));

            executor.ExecuteRoll();

            Assert.That(dice.CurrentFaceIndex, Is.EqualTo(2));
            Assert.That(dice.ActiveRollValue, Is.EqualTo(2));
        }

        [Test]
        public void RollExecutor_RecordsAuthoritativeFactContextAndHistoryOrder()
        {
            DiceRuntimeState enemy = DiceTestFactory.CreateEnemyRuntimeDice(1);
            DiceRuntimeState player = DiceTestFactory.CreatePlayerRuntimeDice(2, 2);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { enemy, player });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, enemy, new SlotId(Side.Enemy, 1));
            PlaceForTest(board, inventory, player, new SlotId(Side.Player, 1));
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 4, 5 }));

            IReadOnlyList<DiceRolledFact> facts = executor.ExecuteRoll().Facts;

            Assert.That(facts[0].Context, Is.EqualTo(new BattleFactContext(1, 1, BattlePhase.Rolling)));
            Assert.That(facts[0].SequenceNumber, Is.EqualTo(1));
            Assert.That(facts[1].SequenceNumber, Is.EqualTo(2));
            Assert.That(facts[0].Side, Is.EqualTo(Side.Enemy));
            Assert.That(facts[1].Side, Is.EqualTo(Side.Player));
            Assert.That(facts[0].RollValue, Is.EqualTo(4));
            Assert.That(facts[1].RollValue, Is.EqualTo(5));
            Assert.That(history.Facts[0], Is.SameAs(facts[0]));
            Assert.That(history.Facts[1], Is.SameAs(facts[1]));
        }

        [Test]
        public void RollExecutor_InvalidUntrackedBoardDiceFailsBeforeAnyRollMutation()
        {
            DiceRuntimeState valid = DiceTestFactory.CreatePlayerRuntimeDice(1, 1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { valid });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, valid, new SlotId(Side.Player, 1));
            board.PlaceDice(new SlotId(Side.Enemy, 6), new DiceInstanceId(999));
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 2 }));

            Assert.Throws<InvalidOperationException>(() => executor.ExecuteRoll());
            Assert.That(valid.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_InvariantFailureDoesNotConsumeOrInvokeFallbackRandomness()
        {
            DiceRuntimeState valid = DiceTestFactory.CreatePlayerRuntimeDice(1, 1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { valid });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, valid, new SlotId(Side.Player, 1));
            board.PlaceDice(new SlotId(Side.Enemy, 6), new DiceInstanceId(999));
            var primary = new ScriptedRandomSource(new[] { 2 });
            var fallback = new ScriptedRandomSource(new[] { 3 });
            var executor = new RollExecutor(battle, board, inventory, history, primary, fallback);

            Assert.Throws<InvalidOperationException>(() => executor.ExecuteRoll());

            Assert.That(primary.RemainingCount, Is.EqualTo(1));
            Assert.That(fallback.RemainingCount, Is.EqualTo(1));
            Assert.That(valid.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_DiceCannotBeOnBoardAndInInventoryAtOnce()
        {
            DiceRuntimeState dice = DiceTestFactory.CreateEnemyRuntimeDice(1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { dice });
            var history = new BattleHistory();
            board.PlaceDice(new SlotId(Side.Enemy, 1), dice.InstanceId);
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 1 }));

            Assert.Throws<InvalidOperationException>(() => executor.ExecuteRoll());
            Assert.That(dice.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void RollExecutor_DecayedBoardDiceCannotRoll()
        {
            DiceRuntimeState dice = DiceTestFactory.CreateEnemyRuntimeDice(1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            battle.ApplyApprovedPhaseTransition(BattlePhase.Rolling);
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { dice });
            var history = new BattleHistory();
            PlaceForTest(board, inventory, dice, new SlotId(Side.Enemy, 1));
            dice.MarkDecayedForCurrentGame();
            var executor = new RollExecutor(battle, board, inventory, history, new ScriptedRandomSource(new[] { 1 }));

            Assert.Throws<InvalidOperationException>(() => executor.ExecuteRoll());
            Assert.That(dice.HasCurrentFace, Is.False);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void ApplyDiceRollCommand_ExecutesOnlyOnce()
        {
            DiceRuntimeState dice = DiceTestFactory.CreatePlayerRuntimeDice(1, 1);
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { dice });
            var slot = new SlotId(Side.Player, 2);
            PlaceForTest(board, inventory, dice, slot);
            var command = new ApplyDiceRollCommand(battle, board, inventory, dice, slot, 3);

            DiceRolledFact fact = command.Execute();

            Assert.That(fact.FaceIndex, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => command.Execute());
        }

        [Test]
        public void ApplyDiceRollCommand_RejectsStaleSlotOccupancyBeforeMutation()
        {
            DiceRuntimeState first = DiceTestFactory.CreatePlayerRuntimeDice(1, 1, "dice.first");
            DiceRuntimeState second = DiceTestFactory.CreatePlayerRuntimeDice(2, 2, "dice.second");
            BattleState battle = DiceTestFactory.CreateBattleState();
            var board = new BoardState();
            var inventory = new BattleInventoryState(10, new[] { first, second });
            var firstSlot = new SlotId(Side.Player, 1);
            var secondSlot = new SlotId(Side.Player, 2);
            PlaceForTest(board, inventory, first, firstSlot);
            PlaceForTest(board, inventory, second, secondSlot);
            var command = new ApplyDiceRollCommand(battle, board, inventory, first, firstSlot, 6);
            board.SwapDice(firstSlot, secondSlot);

            Assert.Throws<InvalidOperationException>(() => command.Execute());
            Assert.That(first.HasCurrentFace, Is.False);
        }

        private static void PlaceForTest(
            BoardState board,
            BattleInventoryState inventory,
            DiceRuntimeState dice,
            SlotId slotId)
        {
            inventory.RemoveFromInventory(dice.InstanceId);
            board.PlaceDice(slotId, dice.InstanceId);
        }
    }
}
