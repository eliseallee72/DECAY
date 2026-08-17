using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns post-SCORE round cleanup: Unstable slots eject their saved dice and break, round-local faces clear,
    /// and the completed round score is folded into the current game score.
    /// </summary>
    public sealed class RoundEndExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly ScoreState _scoreState;
        private readonly BattleHistory _history;
        private readonly GameEndCondition _gameEndCondition;
        private readonly RoundEndCompletionGate _completionGate;

        internal RoundEndExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            ScoreState scoreState,
            BattleHistory history)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _gameEndCondition = new GameEndCondition(_battleState, _boardState);
            _completionGate = new RoundEndCompletionGate(_battleState, _boardState, _battleInventoryState, _scoreState, _gameEndCondition);
        }

        internal RoundEndExecutionResult ExecuteRoundEnd()
        {
            if (_battleState.CurrentPhase != BattlePhase.RoundEnd)
                throw new InvalidOperationException($"Round cleanup requires phase {BattlePhase.RoundEnd}; current phase is {_battleState.CurrentPhase}.");

            ValidateBeforeCommit();
            int firstFactIndex = _history.Count;

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                CleanupUnstable(new SlotId(Side.Enemy, number));
                CleanupUnstable(new SlotId(Side.Player, number));
            }

            for (int i = 0; i < _battleInventoryState.TrackedDiceIds.Count; i++)
            {
                DiceRuntimeState dice = _battleInventoryState.GetDice(_battleInventoryState.TrackedDiceIds[i]);
                new ClearDiceCurrentFaceCommand(dice).Execute();
            }

            RoundScoreCompletion scoreCompletion = new FinalizeRoundScoreCommand(_battleState, _scoreState).Execute();
            bool gameEndRequired = _gameEndCondition.IsGameEndRequired;
            _history.Record(new RoundEndedFact(
                _battleState.CurrentFactContext,
                scoreCompletion.EnemyRoundScore,
                scoreCompletion.PlayerRoundScore,
                scoreCompletion.EnemyGameScore,
                scoreCompletion.PlayerGameScore,
                gameEndRequired));

            var slots = new List<RoundEndSlotSnapshot>(BattleRules.SlotsPerSide * 2);
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                slots.Add(Capture(new SlotId(Side.Enemy, number)));
                slots.Add(Capture(new SlotId(Side.Player, number)));
            }

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++) facts.Add(_history.Facts[i]);
            return new RoundEndExecutionResult(
                _battleState.CurrentFactContext,
                scoreCompletion,
                gameEndRequired,
                slots.AsReadOnly(),
                facts.AsReadOnly());
        }

        internal BattleFlowDenialReason EvaluateCompletion(RoundEndExecutionResult result) => _completionGate.Evaluate(result);

        private void CleanupUnstable(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (slot.Condition != SlotCondition.Unstable) return;
            if (slot.HasDice)
                _history.Record(new ReturnDiceToInventoryCommand(_battleState, _boardState, _battleInventoryState, slotId).Execute());
            _history.Record(new SetSlotConditionCommand(_battleState, _boardState, slotId, SlotCondition.Broken).Execute());
        }

        private void ValidateBeforeCommit()
        {
            _scoreState.RequireCanFinalizeRound();
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ValidateSlot(new SlotId(Side.Enemy, number));
                ValidateSlot(new SlotId(Side.Player, number));
            }
        }

        private void ValidateSlot(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!slot.HasDice) return;
            if (slot.Condition == SlotCondition.Broken)
                throw new InvalidOperationException($"Broken slot {slotId} cannot contain dice at RoundEnd.");
            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Board slot {slotId} contains untracked dice {slot.OccupantDiceId}.");
            if (dice.Owner != slotId.Side || dice.IsDecayedForCurrentGame || _battleInventoryState.IsInInventory(dice.InstanceId))
                throw new InvalidOperationException($"Board dice {dice.InstanceId} has inconsistent authoritative location/state at RoundEnd.");
        }

        private RoundEndSlotSnapshot Capture(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            return new RoundEndSlotSnapshot(slotId, slot.Condition, slot.HasDice, slot.HasDice ? slot.OccupantDiceId : default);
        }
    }
}
