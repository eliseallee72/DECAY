using System;

namespace Decay
{
    /// <summary>
    /// Read-only completion proof for SCORE. It rejects drift instead of repairing it.
    /// </summary>
    internal sealed class ScoreCompletionGate
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly ScoreState _scoreState;

        internal ScoreCompletionGate(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            ScoreState scoreState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
        }

        internal BattleFlowDenialReason Evaluate(ScoreExecutionResult result)
        {
            if (result == null) return BattleFlowDenialReason.ScoreNotResolved;
            if (_battleState.CurrentPhase != BattlePhase.ScoreProcess) return BattleFlowDenialReason.WrongPhase;
            if (result.Context != _battleState.CurrentFactContext) return BattleFlowDenialReason.ScoreResolutionIncomplete;
            if (_scoreState.GetRoundScore(Side.Enemy) != result.EndingEnemyRoundScore
                || _scoreState.GetRoundScore(Side.Player) != result.EndingPlayerRoundScore)
                return BattleFlowDenialReason.ScoreResolutionIncomplete;

            for (int i = 0; i < result.Pairs.Count; i++)
            {
                ScorePairResolution expectedPair = result.Pairs[i];
                if (!Matches(expectedPair.PairId.EnemySlot, expectedPair.Enemy)
                    || !Matches(expectedPair.PairId.PlayerSlot, expectedPair.Player))
                    return BattleFlowDenialReason.ScoreResolutionIncomplete;
            }

            return BattleFlowDenialReason.None;
        }

        private bool Matches(SlotId slotId, ScoreResolution? expected)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!expected.HasValue)
            {
                return !slot.HasDice;
            }

            ScoreResolution resolution = expected.Value;
            if (resolution.SlotId != slotId) return false;
            if (!slot.HasDice || slot.OccupantDiceId != resolution.DiceId || slot.Condition == SlotCondition.Broken)
                return false;
            if (!_battleInventoryState.TryGetDice(resolution.DiceId, out DiceRuntimeState dice)) return false;
            if (_battleInventoryState.IsInInventory(resolution.DiceId) || dice.IsDecayedForCurrentGame || !dice.HasCurrentFace)
                return false;
            if (dice.Owner != resolution.Side || dice.CurrentFaceIndex != resolution.FaceIndex) return false;
            if (dice.ActiveRollValue != resolution.RollValue
                || dice.GeneralScoreValue != resolution.GeneralScoreValue
                || dice.ActiveFaceScoreValue != resolution.FaceScoreValue
                || dice.ActiveScoreContribution != resolution.ScoreContribution)
                return false;
            return true;
        }
    }
}
