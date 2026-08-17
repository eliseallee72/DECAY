using System;

namespace Decay
{
    /// <summary>
    /// Read-only prerequisite for leaving Rolling. It verifies that the successful execution receipt belongs
    /// to this game/round, covers every dice currently participating on the Board, and still matches each
    /// authoritative DiceRuntimeState current face. It never repairs state or chooses fallback behavior.
    /// </summary>
    internal sealed class RollCompletionGate
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;

        internal RollCompletionGate(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
        }

        internal BattleFlowDenialReason Evaluate(RollExecutionResult executionResult)
        {
            if (executionResult == null)
            {
                return BattleFlowDenialReason.RollNotResolved;
            }

            BattleFactContext currentContext = _battleState.CurrentFactContext;
            if (currentContext.Phase != BattlePhase.Rolling
                || executionResult.Context.GameNumber != currentContext.GameNumber
                || executionResult.Context.RoundNumber != currentContext.RoundNumber
                || executionResult.Context.Phase != currentContext.Phase)
            {
                return BattleFlowDenialReason.RollResolutionIncomplete;
            }

            int participatingDiceCount = 0;
            for (int slotNumber = BattleRules.FirstSlotNumber; slotNumber <= BattleRules.LastSlotNumber; slotNumber++)
            {
                if (!ValidateOccupiedSlot(new SlotId(Side.Enemy, slotNumber), executionResult, ref participatingDiceCount)
                    || !ValidateOccupiedSlot(new SlotId(Side.Player, slotNumber), executionResult, ref participatingDiceCount))
                {
                    return BattleFlowDenialReason.RollResolutionIncomplete;
                }
            }

            // A receipt must neither omit a current Board participant nor contain a stale/extra participant.
            return participatingDiceCount == executionResult.Resolutions.Count
                ? BattleFlowDenialReason.None
                : BattleFlowDenialReason.RollResolutionIncomplete;
        }

        private bool ValidateOccupiedSlot(
            SlotId slotId,
            RollExecutionResult executionResult,
            ref int participatingDiceCount)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!slot.HasDice)
            {
                return true;
            }

            participatingDiceCount++;
            if (!executionResult.TryGetResolution(slotId, slot.OccupantDiceId, out RollResolution resolution))
            {
                return false;
            }

            if (!_battleInventoryState.TryGetDice(resolution.DiceId, out DiceRuntimeState diceState))
            {
                return false;
            }

            return diceState.HasCurrentFace && diceState.CurrentFaceIndex == resolution.FaceIndex;
        }
    }
}
