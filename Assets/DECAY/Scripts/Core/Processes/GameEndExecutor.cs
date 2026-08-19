using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Finalizes one game score. When another game remains, it also performs the documented between-game reset:
    /// slot conditions reset, DECAYED Player dice restore from GlobalInventory, and DECAYED Enemy dice restore from
    /// their immutable battle-start seeds. Healthy surviving dice retain battle-local mutations and board position.
    /// </summary>
    public sealed class GameEndExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly GlobalInventoryState _globalInventoryState;
        private readonly EnemyDiceResetSeedCatalog _enemyResetSeeds;
        private readonly ScoreState _scoreState;
        private readonly BattleHistory _history;
        private readonly GameEndCompletionGate _completionGate;

        internal GameEndExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            GlobalInventoryState globalInventoryState,
            EnemyDiceResetSeedCatalog enemyResetSeeds,
            ScoreState scoreState,
            BattleHistory history)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _globalInventoryState = globalInventoryState ?? throw new ArgumentNullException(nameof(globalInventoryState));
            _enemyResetSeeds = enemyResetSeeds ?? throw new ArgumentNullException(nameof(enemyResetSeeds));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _completionGate = new GameEndCompletionGate(_battleState, _boardState, _battleInventoryState, _scoreState);
        }

        internal GameEndExecutionResult ExecuteGameEnd()
        {
            if (_battleState.CurrentPhase != BattlePhase.GameEnd)
                throw new InvalidOperationException($"Game cleanup requires phase {BattlePhase.GameEnd}; current phase is {_battleState.CurrentPhase}.");
            bool prepareNextGame = _battleState.CurrentGameNumber < _battleState.GamesPerBattle;
            List<DiceResetPlanEntry> resetPlan = ValidateBeforeCommit(prepareNextGame);

            int firstFactIndex = _history.Count;
            GameScoreCompletion scoreCompletion = new FinalizeGameScoreCommand(_battleState, _scoreState).Execute();

            if (prepareNextGame)
            {
                ResetSlotConditionsForNextGame();
                RestoreDecayedDiceForNextGame(resetPlan);
            }

            _history.Record(new GameEndedFact(
                _battleState.CurrentFactContext,
                scoreCompletion.EnemyGameScore,
                scoreCompletion.PlayerGameScore,
                scoreCompletion.EnemyBattleScore,
                scoreCompletion.PlayerBattleScore));

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++) facts.Add(_history.Facts[i]);
            return new GameEndExecutionResult(
                _battleState.CurrentFactContext,
                scoreCompletion,
                prepareNextGame,
                facts.AsReadOnly());
        }

        internal BattleFlowDenialReason EvaluateCompletion(GameEndExecutionResult result) => _completionGate.Evaluate(result);

        private void ResetSlotConditionsForNextGame()
        {
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ResetSlot(new SlotId(Side.Enemy, number));
                ResetSlot(new SlotId(Side.Player, number));
            }
        }

        private void ResetSlot(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (slot.Condition == SlotCondition.Unbroken) return;
            if (slot.HasDice && slot.Condition == SlotCondition.Broken)
                throw new InvalidOperationException($"Broken slot {slotId} cannot retain dice during new-game reset.");
            _history.Record(new SetSlotConditionCommand(_battleState, _boardState, slotId, SlotCondition.Unbroken).Execute());
        }

        private void RestoreDecayedDiceForNextGame(IReadOnlyList<DiceResetPlanEntry> resetPlan)
        {
            for (int i = 0; i < resetPlan.Count; i++)
            {
                DiceResetPlanEntry entry = resetPlan[i];
                _history.Record(new ResetDiceForNewGameCommand(
                    _battleState,
                    _battleInventoryState,
                    entry.Dice,
                    entry.Seed).Execute());
            }
        }

        private List<DiceResetPlanEntry> ValidateBeforeCommit(bool prepareNextGame)
        {
            if (_scoreState.GetRoundScore(Side.Enemy) != 0 || _scoreState.GetRoundScore(Side.Player) != 0)
                throw new InvalidOperationException("Round score must already be finalized before GameEnd.");
            _scoreState.RequireCanFinalizeGame();

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ValidateSlot(new SlotId(Side.Enemy, number));
                ValidateSlot(new SlotId(Side.Player, number));
            }

            var resetPlan = new List<DiceResetPlanEntry>();
            for (int i = 0; i < _battleInventoryState.TrackedDiceIds.Count; i++)
            {
                DiceRuntimeState dice = _battleInventoryState.GetDice(_battleInventoryState.TrackedDiceIds[i]);
                bool onBoard = _boardState.IsDiceOnBoard(dice.InstanceId);
                bool inInventory = _battleInventoryState.IsInInventory(dice.InstanceId);
                if (dice.IsDecayedForCurrentGame)
                {
                    if (onBoard || inInventory)
                        throw new InvalidOperationException($"DECAYED dice {dice.InstanceId} must be tracked but unavailable at GameEnd.");
                    if (prepareNextGame)
                        resetPlan.Add(new DiceResetPlanEntry(dice, ResolveResetSeed(dice)));
                }
                else if (onBoard == inInventory)
                {
                    throw new InvalidOperationException($"Active dice {dice.InstanceId} must be in exactly one authoritative location at GameEnd.");
                }

                if (dice.HasCurrentFace)
                    throw new InvalidOperationException($"Dice {dice.InstanceId} still has a round-local face at GameEnd.");
            }

            return resetPlan;
        }

        private void ValidateSlot(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (slot.Condition == SlotCondition.Unstable)
                throw new InvalidOperationException($"Unstable slot {slotId} must be resolved during RoundEnd before GameEnd.");
            if (!slot.HasDice) return;
            if (slot.Condition != SlotCondition.Unbroken)
                throw new InvalidOperationException($"Occupied GameEnd slot {slotId} must be Unbroken.");
            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Board slot {slotId} contains untracked dice {slot.OccupantDiceId}.");
            if (dice.Owner != slotId.Side || dice.IsDecayedForCurrentGame || _battleInventoryState.IsInInventory(dice.InstanceId))
                throw new InvalidOperationException($"Board dice {dice.InstanceId} has inconsistent authoritative location/state at GameEnd.");
        }

        private DiceRuntimeSeed ResolveResetSeed(DiceRuntimeState dice)
        {
            DiceRuntimeSeed seed;
            if (dice.Owner == Side.Player)
            {
                if (!dice.HasSourceOwnedDice || !dice.SourceOwnedDiceId.IsValid)
                    throw new InvalidOperationException($"Player dice {dice.InstanceId} lacks its permanent Global Inventory source.");
                if (!_globalInventoryState.TryGetDice(dice.SourceOwnedDiceId, out GlobalDiceState globalDice))
                    throw new InvalidOperationException($"Global Inventory no longer contains source {dice.SourceOwnedDiceId} for Player dice {dice.InstanceId}.");
                seed = globalDice.BattleSeed;
            }
            else
            {
                if (!_enemyResetSeeds.TryGet(dice.InstanceId, out seed))
                    throw new InvalidOperationException($"Enemy dice {dice.InstanceId} lacks its immutable battle-start reset seed.");
            }

            if (seed.DefinitionId != dice.DefinitionId)
                throw new InvalidOperationException($"Reset source definition {seed.DefinitionId} does not match dice {dice.InstanceId} definition {dice.DefinitionId}.");
            if (!seed.TryValidate(out string error))
                throw new InvalidOperationException($"Reset source for dice {dice.InstanceId} is invalid: {error}");
            return seed;
        }

        private readonly struct DiceResetPlanEntry
        {
            internal DiceResetPlanEntry(DiceRuntimeState dice, DiceRuntimeSeed seed)
            {
                Dice = dice ?? throw new ArgumentNullException(nameof(dice));
                Seed = seed ?? throw new ArgumentNullException(nameof(seed));
            }
            internal DiceRuntimeState Dice { get; }
            internal DiceRuntimeSeed Seed { get; }
        }

    }
}
