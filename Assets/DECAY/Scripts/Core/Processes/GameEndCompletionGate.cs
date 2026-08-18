using System;

namespace Decay
{
    internal sealed class GameEndCompletionGate
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly ScoreState _scoreState;

        internal GameEndCompletionGate(
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

        internal BattleFlowDenialReason Evaluate(GameEndExecutionResult result)
        {
            if (result == null) return BattleFlowDenialReason.GameEndNotResolved;
            if (_battleState.CurrentPhase != BattlePhase.GameEnd) return BattleFlowDenialReason.WrongPhase;
            if (result.Context != _battleState.CurrentFactContext) return BattleFlowDenialReason.GameEndResolutionIncomplete;
            if (_scoreState.GetRoundScore(Side.Enemy) != 0 || _scoreState.GetRoundScore(Side.Player) != 0
                || _scoreState.GetGameScore(Side.Enemy) != 0 || _scoreState.GetGameScore(Side.Player) != 0)
                return BattleFlowDenialReason.GameEndResolutionIncomplete;
            if (_scoreState.GetBattleScore(Side.Enemy) != result.ScoreCompletion.EnemyBattleScore
                || _scoreState.GetBattleScore(Side.Player) != result.ScoreCompletion.PlayerBattleScore)
                return BattleFlowDenialReason.GameEndResolutionIncomplete;
            bool shouldPrepareNextGame = _battleState.CurrentGameNumber < _battleState.GamesPerBattle;
            if (result.PreparedNextGame != shouldPrepareNextGame)
                return BattleFlowDenialReason.GameEndResolutionIncomplete;

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                SlotState enemySlot = _boardState.GetSlot(new SlotId(Side.Enemy, number));
                SlotState playerSlot = _boardState.GetSlot(new SlotId(Side.Player, number));
                if (enemySlot.Condition == SlotCondition.Unstable || playerSlot.Condition == SlotCondition.Unstable)
                    return BattleFlowDenialReason.GameEndResolutionIncomplete;
                if (result.PreparedNextGame
                    && (enemySlot.Condition != SlotCondition.Unbroken || playerSlot.Condition != SlotCondition.Unbroken))
                    return BattleFlowDenialReason.GameEndResolutionIncomplete;
            }

            for (int i = 0; i < _battleInventoryState.TrackedDiceIds.Count; i++)
            {
                DiceRuntimeState dice = _battleInventoryState.GetDice(_battleInventoryState.TrackedDiceIds[i]);
                if (dice.HasCurrentFace) return BattleFlowDenialReason.GameEndResolutionIncomplete;
                bool onBoard = _boardState.IsDiceOnBoard(dice.InstanceId);
                bool inInventory = _battleInventoryState.IsInInventory(dice.InstanceId);
                if (result.PreparedNextGame)
                {
                    if (dice.IsDecayedForCurrentGame || onBoard == inInventory)
                        return BattleFlowDenialReason.GameEndResolutionIncomplete;
                }
                else if (dice.IsDecayedForCurrentGame)
                {
                    if (onBoard || inInventory)
                        return BattleFlowDenialReason.GameEndResolutionIncomplete;
                }
                else if (onBoard == inInventory)
                {
                    return BattleFlowDenialReason.GameEndResolutionIncomplete;
                }
            }

            return BattleFlowDenialReason.None;
        }
    }
}
