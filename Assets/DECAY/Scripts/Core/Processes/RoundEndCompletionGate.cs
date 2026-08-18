using System;

namespace Decay
{
    internal sealed class RoundEndCompletionGate
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly ScoreState _scoreState;
        private readonly GameEndCondition _gameEndCondition;

        internal RoundEndCompletionGate(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            ScoreState scoreState,
            GameEndCondition gameEndCondition)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _gameEndCondition = gameEndCondition ?? throw new ArgumentNullException(nameof(gameEndCondition));
        }

        internal BattleFlowDenialReason Evaluate(RoundEndExecutionResult result)
        {
            if (result == null) return BattleFlowDenialReason.RoundEndNotResolved;
            if (_battleState.CurrentPhase != BattlePhase.RoundEnd) return BattleFlowDenialReason.WrongPhase;
            if (result.Context != _battleState.CurrentFactContext) return BattleFlowDenialReason.RoundEndResolutionIncomplete;
            if (_scoreState.GetRoundScore(Side.Enemy) != 0 || _scoreState.GetRoundScore(Side.Player) != 0)
                return BattleFlowDenialReason.RoundEndResolutionIncomplete;
            if (_scoreState.GetGameScore(Side.Enemy) != result.ScoreCompletion.EnemyGameScore
                || _scoreState.GetGameScore(Side.Player) != result.ScoreCompletion.PlayerGameScore)
                return BattleFlowDenialReason.RoundEndResolutionIncomplete;
            if (_gameEndCondition.IsGameEndRequired != result.GameEndRequired)
                return BattleFlowDenialReason.RoundEndResolutionIncomplete;

            for (int i = 0; i < result.Slots.Count; i++)
            {
                RoundEndSlotSnapshot expected = result.Slots[i];
                SlotState current = _boardState.GetSlot(expected.SlotId);
                if (current.Condition != expected.Condition || current.HasDice != expected.HasDice)
                    return BattleFlowDenialReason.RoundEndResolutionIncomplete;
                if (current.HasDice && current.OccupantDiceId != expected.OccupantDiceId)
                    return BattleFlowDenialReason.RoundEndResolutionIncomplete;
                if (current.Condition == SlotCondition.Unstable)
                    return BattleFlowDenialReason.RoundEndResolutionIncomplete;
            }

            for (int i = 0; i < _battleInventoryState.TrackedDiceIds.Count; i++)
            {
                DiceRuntimeState dice = _battleInventoryState.GetDice(_battleInventoryState.TrackedDiceIds[i]);
                if (dice.HasCurrentFace) return BattleFlowDenialReason.RoundEndResolutionIncomplete;
            }

            return BattleFlowDenialReason.None;
        }
    }
}
