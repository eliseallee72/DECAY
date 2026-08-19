using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Decay.Tests
{
    public sealed class ScoreLifecycleTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null) Object.DestroyImmediate(_createdObjects[i]);
            }
            _createdObjects.Clear();
        }

        [Test]
        public void ScoreState_TracksRoundGameBattleAndDisplayTotalsWithoutDoubleCounting()
        {
            var score = new ScoreState();
            score.ApplyContribution(Side.Player, 4);
            score.ApplyContribution(Side.Enemy, 2);
            Assert.That(score.GetTotalScore(Side.Player), Is.EqualTo(4));
            Assert.That(score.GetTotalScore(Side.Enemy), Is.EqualTo(2));

            RoundScoreCompletion round = score.FinalizeRound();
            Assert.That(round.PlayerRoundScore, Is.EqualTo(4));
            Assert.That(score.GetRoundScore(Side.Player), Is.Zero);
            Assert.That(score.GetGameScore(Side.Player), Is.EqualTo(4));
            Assert.That(score.GetTotalScore(Side.Player), Is.EqualTo(4));

            score.ApplyContribution(Side.Player, 3);
            score.FinalizeRound();
            GameScoreCompletion game = score.FinalizeGame();
            Assert.That(game.PlayerGameScore, Is.EqualTo(7));
            Assert.That(score.GetGameScore(Side.Player), Is.Zero);
            Assert.That(score.GetBattleScore(Side.Player), Is.EqualTo(7));
            Assert.That(score.GetTotalScore(Side.Player), Is.EqualTo(7));
        }

        [Test]
        public void ScoreState_TotalOverflowRejectsContributionBeforeRoundScoreMutation()
        {
            var score = new ScoreState();
            score.ApplyContribution(Side.Player, int.MaxValue);
            score.FinalizeRound();
            score.FinalizeGame();

            Assert.Throws<System.OverflowException>(() => score.ApplyContribution(Side.Player, 1));
            Assert.That(score.GetRoundScore(Side.Player), Is.Zero);
            Assert.That(score.GetBattleScore(Side.Player), Is.EqualTo(int.MaxValue));
            Assert.That(score.GetTotalScore(Side.Player), Is.EqualTo(int.MaxValue));
        }

        [TestCase(10, 3, 13)]
        [TestCase(-5, 2, -3)]
        [TestCase(0, -4, -4)]
        public void ScoreResolver_AddsGeneralAndActiveFaceScore(int generalScore, int faceScore, int expected)
        {
            ScoreFixture f = CreateScoreFixture(generalScore, faceScore);
            ScoreExecutionResult result = f.Executor.ExecuteScore();

            Assert.That(result.EndingPlayerRoundScore, Is.EqualTo(expected));
            ScoreAppliedFact fact = f.History.Facts.OfType<ScoreAppliedFact>().Single();
            Assert.That(fact.GeneralScoreValue, Is.EqualTo(generalScore));
            Assert.That(fact.FaceScoreValue, Is.EqualTo(faceScore));
            Assert.That(fact.AppliedScore, Is.EqualTo(expected));
            Assert.That(fact.ResultingTotalScore, Is.EqualTo(expected));
        }

        [Test]
        public void ScoreExecutor_UnstableSavedDiceStillScores()
        {
            ScoreFixture f = CreateScoreFixture(2, 5);
            f.Board.SetSlotCondition(f.PlayerSlot, SlotCondition.Unstable);

            ScoreExecutionResult result = f.Executor.ExecuteScore();

            Assert.That(result.EndingPlayerRoundScore, Is.EqualTo(7));
            Assert.That(f.Score.GetRoundScore(Side.Player), Is.EqualTo(7));
        }

        [Test]
        public void ScoreExecutor_RecordsSlotPairsOneThroughSixWithEnemyThenPlayerFactTieBreak()
        {
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeState enemy1 = DiceTestFactory.CreateEnemyRuntimeDice(1);
            DiceRuntimeState player1 = DiceTestFactory.CreatePlayerRuntimeDice(2, 102);
            DiceRuntimeState enemy3 = DiceTestFactory.CreateEnemyRuntimeDice(3);
            DiceRuntimeState player6 = DiceTestFactory.CreatePlayerRuntimeDice(4, 104);
            var inventory = new BattleInventoryState(10, new[] { enemy1, player1, enemy3, player6 });
            var history = new BattleHistory();
            var score = new ScoreState();
            EnterScorePhase(state);
            PlaceRolled(state, board, inventory, enemy1, new SlotId(Side.Enemy, 1), 2);
            PlaceRolled(state, board, inventory, player1, new SlotId(Side.Player, 1), 3);
            PlaceRolled(state, board, inventory, enemy3, new SlotId(Side.Enemy, 3), 4);
            PlaceRolled(state, board, inventory, player6, new SlotId(Side.Player, 6), 5);
            var executor = new ScoreExecutor(state, board, inventory, score, history);

            executor.ExecuteScore();

            ScoreAppliedFact[] facts = history.Facts.OfType<ScoreAppliedFact>().ToArray();
            Assert.That(facts.Select(x => x.SlotId).ToArray(), Is.EqualTo(new[]
            {
                new SlotId(Side.Enemy, 1),
                new SlotId(Side.Player, 1),
                new SlotId(Side.Enemy, 3),
                new SlotId(Side.Player, 6)
            }));
        }

        [Test]
        public void ScoreExecutor_ValidatesEntireBoardBeforeFirstScoreMutation()
        {
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeState valid = DiceTestFactory.CreatePlayerRuntimeDice(1, 101);
            DiceRuntimeState invalid = DiceTestFactory.CreatePlayerRuntimeDice(2, 102);
            var inventory = new BattleInventoryState(10, new[] { valid, invalid });
            var history = new BattleHistory();
            var score = new ScoreState();
            EnterScorePhase(state);
            PlaceRolled(state, board, inventory, valid, new SlotId(Side.Player, 1), 5);
            inventory.RemoveFromInventory(invalid.InstanceId);
            board.PlaceDice(new SlotId(Side.Player, 6), invalid.InstanceId); // deliberately has no current face
            var executor = new ScoreExecutor(state, board, inventory, score, history);

            Assert.Throws<System.InvalidOperationException>(() => executor.ExecuteScore());
            Assert.That(score.GetRoundScore(Side.Player), Is.Zero);
            Assert.That(history.Count, Is.Zero);
        }

        [Test]
        public void ScoreCompletionGate_RejectsFaceDriftAfterCommittedScore()
        {
            ScoreFixture f = CreateScoreFixture(0, 5);
            ScoreExecutionResult result = f.Executor.ExecuteScore();
            f.Player.SetCurrentFace(4);

            Assert.That(f.Executor.EvaluateCompletion(result), Is.EqualTo(BattleFlowDenialReason.ScoreResolutionIncomplete));
        }

        [Test]
        public void SavedSix_ScoresBeforeRoundEndEjectsItToInventory()
        {
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeState savior = DiceTestFactory.CreatePlayerRuntimeDice(1, 101);
            DiceRuntimeState savedSix = DiceTestFactory.CreatePlayerRuntimeDice(2, 102);
            var inventory = new BattleInventoryState(10, new[] { savior, savedSix });
            var history = new BattleHistory();
            var score = new ScoreState();
            EnterDecayPhase(state);
            PlaceRolled(state, board, inventory, savior, new SlotId(Side.Player, 1), 1);
            PlaceRolled(state, board, inventory, savedSix, new SlotId(Side.Player, 2), 6);
            var decay = new DecayExecutor(state, board, inventory, history);

            DecayExecutionResult decayResult = decay.ExecuteDecay();
            Assert.That(decay.EvaluateCompletion(decayResult), Is.EqualTo(BattleFlowDenialReason.None));
            Assert.That(savedSix.IsDecayedForCurrentGame, Is.False);
            Assert.That(board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Unstable));

            new AdvancePhaseCommand(state, BattlePhase.ScoreProcess).Execute();
            var scoring = new ScoreExecutor(state, board, inventory, score, history);
            ScoreExecutionResult scoreResult = scoring.ExecuteScore();
            Assert.That(scoreResult.EndingPlayerRoundScore, Is.EqualTo(7), "Both surviving 1 and SAVED 6 score.");

            new AdvancePhaseCommand(state, BattlePhase.RoundEnd).Execute();
            var roundEnd = new RoundEndExecutor(state, board, inventory, score, history);
            RoundEndExecutionResult round = roundEnd.ExecuteRoundEnd();

            Assert.That(round.ScoreCompletion.PlayerRoundScore, Is.EqualTo(7));
            Assert.That(inventory.IsInInventory(savedSix.InstanceId), Is.True);
            Assert.That(board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Broken));
            Assert.That(savedSix.IsDecayedForCurrentGame, Is.False);
        }

        [Test]
        public void DecayedSix_IsRemovedBeforeScoreAndContributesNothing()
        {
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeState six = DiceTestFactory.CreatePlayerRuntimeDice(1, 101);
            var inventory = new BattleInventoryState(10, new[] { six });
            var history = new BattleHistory();
            var score = new ScoreState();
            EnterDecayPhase(state);
            PlaceRolled(state, board, inventory, six, new SlotId(Side.Player, 2), 6);
            var decay = new DecayExecutor(state, board, inventory, history);
            decay.ExecuteDecay();

            new AdvancePhaseCommand(state, BattlePhase.ScoreProcess).Execute();
            var scoring = new ScoreExecutor(state, board, inventory, score, history);
            ScoreExecutionResult scoreResult = scoring.ExecuteScore();

            Assert.That(scoreResult.EndingPlayerRoundScore, Is.Zero);
            Assert.That(history.Facts.OfType<ScoreAppliedFact>().Any(x => x.DiceId == six.InstanceId), Is.False);
        }

        [Test]
        public void RoundEnd_UnstableDiceScoresThenReturnsToInventoryAndSlotBreaksWithoutResettingDice()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterRoundEndDirect();
            DiceRuntimeState player = f.Player;
            player.SetGeneralScoreValue(9);
            player.SetCurrentFace(5);
            f.Inventory.RemoveFromInventory(player.InstanceId);
            f.Board.PlaceDice(new SlotId(Side.Player, 2), player.InstanceId);
            f.Board.SetSlotCondition(new SlotId(Side.Player, 2), SlotCondition.Unstable);
            f.Score.ApplyContribution(Side.Player, player.ActiveScoreContribution);

            RoundEndExecutionResult result = f.RoundEnd.ExecuteRoundEnd();

            Assert.That(result.ScoreCompletion.PlayerRoundScore, Is.EqualTo(14));
            Assert.That(f.Inventory.IsInInventory(player.InstanceId), Is.True);
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).Condition, Is.EqualTo(SlotCondition.Broken));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Player, 2)).HasDice, Is.False);
            Assert.That(player.GeneralScoreValue, Is.EqualTo(9), "SAVED dice must retain battle-local mutation.");
            Assert.That(player.HasCurrentFace, Is.False);
        }

        [Test]
        public void RoundEnd_HealthySurvivorKeepsBoardPositionAndOnlyClearsRoundFace()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterRoundEndDirect();
            DiceRuntimeState player = f.Player;
            player.SetGeneralScoreValue(8);
            player.SetCurrentFace(4);
            f.Inventory.RemoveFromInventory(player.InstanceId);
            SlotId slot = new SlotId(Side.Player, 4);
            f.Board.PlaceDice(slot, player.InstanceId);

            f.RoundEnd.ExecuteRoundEnd();

            Assert.That(f.Board.GetSlot(slot).OccupantDiceId, Is.EqualTo(player.InstanceId));
            Assert.That(f.Inventory.IsInInventory(player.InstanceId), Is.False);
            Assert.That(player.GeneralScoreValue, Is.EqualTo(8));
            Assert.That(player.HasCurrentFace, Is.False);
        }

        [Test]
        public void RoundEnd_EmptyUnstableSlotStillBreaks()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterRoundEndDirect();
            SlotId slot = new SlotId(Side.Enemy, 5);
            f.Board.SetSlotCondition(slot, SlotCondition.Unstable);

            f.RoundEnd.ExecuteRoundEnd();

            Assert.That(f.Board.GetSlot(slot).Condition, Is.EqualTo(SlotCondition.Broken));
        }

        [Test]
        public void RoundEnd_EvaluatesBoardBreakGameEndAfterUnstableCleanup()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterRoundEndDirect();
            for (int i = 1; i <= 5; i++)
                f.Board.SetSlotCondition(new SlotId(Side.Player, i), SlotCondition.Broken);
            f.Board.SetSlotCondition(new SlotId(Side.Player, 6), SlotCondition.Unstable);

            RoundEndExecutionResult result = f.RoundEnd.ExecuteRoundEnd();

            Assert.That(result.GameEndRequired, Is.True);
            Assert.That(f.Board.AreAllSlotsBroken(Side.Player), Is.True);
        }

        [Test]
        public void GameEnd_BetweenGamesRestoresDecayedPlayerFromGlobalAndEnemyFromBattleStartSeed()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterGameEndDirect();
            f.Player.SetGeneralScoreValue(77);
            f.Enemy.SetGeneralScoreValue(88);
            f.Player.MarkDecayedForCurrentGame();
            f.Enemy.MarkDecayedForCurrentGame();
            // Both dice begin in inventory in this direct fixture; make them unavailable like DECAY would.
            f.Inventory.RemoveFromInventory(f.Player.InstanceId);
            f.Inventory.RemoveFromInventory(f.Enemy.InstanceId);

            GameEndExecutionResult result = f.GameEnd.ExecuteGameEnd();

            Assert.That(result.PreparedNextGame, Is.True);
            Assert.That(f.Player.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Enemy.IsDecayedForCurrentGame, Is.False);
            Assert.That(f.Player.GeneralScoreValue, Is.EqualTo(0));
            Assert.That(f.Enemy.GeneralScoreValue, Is.EqualTo(0));
            Assert.That(f.Inventory.IsInInventory(f.Player.InstanceId), Is.True);
            Assert.That(f.Inventory.IsInInventory(f.Enemy.InstanceId), Is.True);
        }

        [Test]
        public void GameEnd_MissingEnemyResetSourceFailsBeforeGameScoreOrResetCommits()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterGameEndDirect();
            f.Enemy.MarkDecayedForCurrentGame();
            f.Inventory.RemoveFromInventory(f.Enemy.InstanceId);
            f.Score.ApplyContribution(Side.Player, 5);
            f.Score.FinalizeRound();
            var global = new GlobalInventoryState(new[]
            {
                new GlobalDiceState(new OwnedDiceId(101), CreateNeutralSeed("dice.lifecycle_player"))
            });
            var emptyEnemyReset = new EnemyDiceResetSeedCatalog(
                new KeyValuePair<DiceInstanceId, DiceRuntimeSeed>[0]);
            var executor = new GameEndExecutor(
                f.State,
                f.Board,
                f.Inventory,
                global,
                emptyEnemyReset,
                f.Score,
                f.History);

            Assert.Throws<System.InvalidOperationException>(() => executor.ExecuteGameEnd());
            Assert.That(f.Score.GetGameScore(Side.Player), Is.EqualTo(5));
            Assert.That(f.Score.GetBattleScore(Side.Player), Is.Zero);
            Assert.That(f.Enemy.IsDecayedForCurrentGame, Is.True);
            Assert.That(f.Inventory.IsInInventory(f.Enemy.InstanceId), Is.False);
        }

        [Test]
        public void GameEnd_BetweenGamesResetsSlotConditionsButPreservesHealthySurvivorAndBattleMutation()
        {
            LifecycleFixture f = CreateLifecycleFixture();
            f.EnterGameEndDirect();
            f.Player.SetGeneralScoreValue(12);
            f.Inventory.RemoveFromInventory(f.Player.InstanceId);
            SlotId survivorSlot = new SlotId(Side.Player, 2);
            f.Board.PlaceDice(survivorSlot, f.Player.InstanceId);
            f.Board.SetSlotCondition(new SlotId(Side.Enemy, 2), SlotCondition.Broken);

            f.GameEnd.ExecuteGameEnd();

            Assert.That(f.Board.GetSlot(survivorSlot).OccupantDiceId, Is.EqualTo(f.Player.InstanceId));
            Assert.That(f.Board.GetSlot(new SlotId(Side.Enemy, 2)).Condition, Is.EqualTo(SlotCondition.Unbroken));
            Assert.That(f.Player.GeneralScoreValue, Is.EqualTo(12));
            Assert.That(f.Inventory.IsInInventory(f.Player.InstanceId), Is.False);
        }

        [Test]
        public void CompleteBattleLoop_TwoGamesFourRoundsScoresSurvivorAndEndsWithWinner()
        {
            DiceDefinition playerDefinition = Track(DiceTestFactory.CreateDefinition("dice.loop_player"));
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            var global = new GlobalInventoryState(new[]
            {
                new GlobalDiceState(new OwnedDiceId(500), DiceRuntimeSeed.FromDefinition(playerDefinition))
            });
            var rolls = Enumerable.Repeat(5, 8).ToArray();
            BattleRuntime runtime = new BattleBootstrapper().Create(
                config,
                global,
                new[] { new OwnedDiceId(500) },
                new DiceRuntimeSeed[0],
                new ScriptedRandomSource(rolls),
                new SeededRandomSource(9));
            DiceInstanceId playerId = runtime.BattleInventoryState.TrackedDiceIds.Single();

            for (int completedRound = 0; completedRound < 8; completedRound++)
            {
                Assert.That(runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.Setup));
                if (completedRound == 0)
                {
                    Assert.That(runtime.MoveDiceController.RequestMove(new MoveDiceRequest(
                        Side.Player,
                        playerId,
                        MoveDiceTarget.Board(new SlotId(Side.Player, 3)))).IsApproved, Is.True);
                }

                Assert.That(runtime.BattleController.RequestRoll().IsApproved, Is.True);
                Assert.That(runtime.BattleController.CompleteRoll().IsApproved, Is.True);
                Assert.That(runtime.BattleController.CompleteEnemyReposition().IsApproved, Is.True);
                Assert.That(runtime.BattleController.RequestDecay().IsApproved, Is.True);
                Assert.That(runtime.BattleController.CompleteDecay().IsApproved, Is.True);
                Assert.That(runtime.BattleController.CompleteScore().IsApproved, Is.True);
                Assert.That(runtime.BattleController.CompleteRoundEnd().IsApproved, Is.True);

                if (runtime.BattleState.CurrentPhase == BattlePhase.GameEnd)
                    Assert.That(runtime.BattleController.CompleteGameEnd().IsApproved, Is.True);
            }

            Assert.That(runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.BattleEnd));
            Assert.That(runtime.ScoreState.GetBattleScore(Side.Player), Is.EqualTo(40));
            Assert.That(runtime.ScoreState.GetBattleScore(Side.Enemy), Is.Zero);
            Assert.That(runtime.ScoreState.GetCurrentBattleOutcome(), Is.EqualTo(BattleOutcome.PlayerWin));
            Assert.That(runtime.History.Facts.OfType<RoundEndedFact>().Count(), Is.EqualTo(8));
            Assert.That(runtime.History.Facts.OfType<GameEndedFact>().Count(), Is.EqualTo(2));
            BattleEndedFact battleEnded = runtime.History.Facts.OfType<BattleEndedFact>().Single();
            Assert.That(battleEnded.Outcome, Is.EqualTo(BattleOutcome.PlayerWin));
        }

        [Test]
        public void CompleteRoundEnd_AllBrokenSideEndsGameAfterScoreCleanupRatherThanStartingAnotherRound()
        {
            BattleConfig config = Track(ScriptableObject.CreateInstance<BattleConfig>());
            BattleRuntime runtime = new BattleBootstrapper().Create(
                config,
                new GlobalInventoryState(new GlobalDiceState[0]),
                new OwnedDiceId[0],
                new DiceRuntimeSeed[0],
                new SeededRandomSource(1),
                new SeededRandomSource(2));
            for (int number = 1; number <= 6; number++)
                runtime.BoardState.SetSlotCondition(new SlotId(Side.Player, number), SlotCondition.Broken);

            Assert.That(runtime.BattleController.RequestRoll().IsApproved, Is.True);
            Assert.That(runtime.BattleController.CompleteRoll().IsApproved, Is.True);
            Assert.That(runtime.BattleController.CompleteEnemyReposition().IsApproved, Is.True);
            Assert.That(runtime.BattleController.RequestDecay().IsApproved, Is.True);
            Assert.That(runtime.BattleController.CompleteDecay().IsApproved, Is.True);
            Assert.That(runtime.BattleController.CompleteScore().IsApproved, Is.True);
            Assert.That(runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.RoundEnd));

            BattleFlowResult roundEnd = runtime.BattleController.CompleteRoundEnd();

            Assert.That(roundEnd.IsApproved, Is.True);
            Assert.That(runtime.BattleState.CurrentPhase, Is.EqualTo(BattlePhase.GameEnd));
            Assert.That(runtime.BattleState.CurrentRoundNumber, Is.EqualTo(1));
        }

        private ScoreFixture CreateScoreFixture(int generalScore, int faceScore)
        {
            DiceDefinition definition = Track(DiceTestFactory.CreateDefinition(
                "dice.score_fixture",
                generalScoreValue: generalScore,
                faceScoreValues: new[] { faceScore, faceScore, faceScore, faceScore, faceScore, faceScore }));
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeState player = DiceRuntimeState.CreatePlayerDice(new DiceInstanceId(1), new OwnedDiceId(1), definition);
            var inventory = new BattleInventoryState(10, new[] { player });
            var score = new ScoreState();
            var history = new BattleHistory();
            EnterScorePhase(state);
            SlotId slot = new SlotId(Side.Player, 2);
            PlaceRolled(state, board, inventory, player, slot, 2);
            return new ScoreFixture(state, board, inventory, player, slot, score, history, new ScoreExecutor(state, board, inventory, score, history));
        }

        private LifecycleFixture CreateLifecycleFixture()
        {
            BattleState state = DiceTestFactory.CreateBattleState();
            BoardState board = new BoardState();
            DiceRuntimeSeed playerSeed = CreateNeutralSeed("dice.lifecycle_player");
            DiceRuntimeSeed enemySeed = CreateNeutralSeed("dice.lifecycle_enemy");
            DiceRuntimeState player = DiceRuntimeState.CreatePlayerDice(new DiceInstanceId(1), new OwnedDiceId(101), playerSeed);
            DiceRuntimeState enemy = DiceRuntimeState.CreateEnemyDice(new DiceInstanceId(2), enemySeed);
            var inventory = new BattleInventoryState(10, new[] { player, enemy });
            var global = new GlobalInventoryState(new[] { new GlobalDiceState(new OwnedDiceId(101), playerSeed) });
            var enemyReset = new EnemyDiceResetSeedCatalog(new[]
            {
                new KeyValuePair<DiceInstanceId, DiceRuntimeSeed>(enemy.InstanceId, enemySeed)
            });
            var score = new ScoreState();
            var history = new BattleHistory();
            return new LifecycleFixture(
                state,
                board,
                inventory,
                player,
                enemy,
                score,
                history,
                new RoundEndExecutor(state, board, inventory, score, history),
                new GameEndExecutor(state, board, inventory, global, enemyReset, score, history));
        }

        private static void EnterDecayPhase(BattleState state)
        {
            new AdvancePhaseCommand(state, BattlePhase.Rolling).Execute();
            new AdvancePhaseCommand(state, BattlePhase.EnemyReposition).Execute();
            new AdvancePhaseCommand(state, BattlePhase.PlayerReposition).Execute();
            new AdvancePhaseCommand(state, BattlePhase.DecayProcess).Execute();
        }

        private static void EnterScorePhase(BattleState state)
        {
            EnterDecayPhase(state);
            new AdvancePhaseCommand(state, BattlePhase.ScoreProcess).Execute();
        }

        private static void PlaceRolled(
            BattleState state,
            BoardState board,
            BattleInventoryState inventory,
            DiceRuntimeState dice,
            SlotId slot,
            int faceIndex)
        {
            inventory.RemoveFromInventory(dice.InstanceId);
            board.PlaceDice(slot, dice.InstanceId);
            dice.SetCurrentFace(faceIndex);
        }

        private static DiceRuntimeSeed CreateNeutralSeed(string definitionId)
        {
            var faces = new DiceFaceSeed[6];
            for (int i = 0; i < faces.Length; i++) faces[i] = new DiceFaceSeed(i + 1, i + 1, i + 1);
            return new DiceRuntimeSeed(new DiceId(definitionId), 0, faces);
        }

        private T Track<T>(T value) where T : Object
        {
            _createdObjects.Add(value);
            return value;
        }

        private sealed class ScoreFixture
        {
            internal ScoreFixture(
                BattleState state,
                BoardState board,
                BattleInventoryState inventory,
                DiceRuntimeState player,
                SlotId playerSlot,
                ScoreState score,
                BattleHistory history,
                ScoreExecutor executor)
            {
                State = state;
                Board = board;
                Inventory = inventory;
                Player = player;
                PlayerSlot = playerSlot;
                Score = score;
                History = history;
                Executor = executor;
            }
            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal DiceRuntimeState Player { get; }
            internal SlotId PlayerSlot { get; }
            internal ScoreState Score { get; }
            internal BattleHistory History { get; }
            internal ScoreExecutor Executor { get; }
        }

        private sealed class LifecycleFixture
        {
            internal LifecycleFixture(
                BattleState state,
                BoardState board,
                BattleInventoryState inventory,
                DiceRuntimeState player,
                DiceRuntimeState enemy,
                ScoreState score,
                BattleHistory history,
                RoundEndExecutor roundEnd,
                GameEndExecutor gameEnd)
            {
                State = state;
                Board = board;
                Inventory = inventory;
                Player = player;
                Enemy = enemy;
                Score = score;
                History = history;
                RoundEnd = roundEnd;
                GameEnd = gameEnd;
            }
            internal BattleState State { get; }
            internal BoardState Board { get; }
            internal BattleInventoryState Inventory { get; }
            internal DiceRuntimeState Player { get; }
            internal DiceRuntimeState Enemy { get; }
            internal ScoreState Score { get; }
            internal BattleHistory History { get; }
            internal RoundEndExecutor RoundEnd { get; }
            internal GameEndExecutor GameEnd { get; }

            internal void EnterRoundEndDirect()
            {
                EnterScorePhase(State);
                new AdvancePhaseCommand(State, BattlePhase.RoundEnd).Execute();
            }

            internal void EnterGameEndDirect()
            {
                EnterRoundEndDirect();
                new AdvancePhaseCommand(State, BattlePhase.GameEnd).Execute();
            }
        }
    }
}
