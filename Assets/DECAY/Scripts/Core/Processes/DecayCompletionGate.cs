using System;

namespace Decay
{
    /// <summary>
    /// Read-only prerequisite for leaving DecayProcess. It verifies that the receipt belongs to the
    /// active round and that each final slot/dice result still matches authoritative state.
    /// </summary>
    internal sealed class DecayCompletionGate
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;

        internal DecayCompletionGate(BattleState battleState, BoardState boardState, BattleInventoryState battleInventoryState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
        }

        internal BattleFlowDenialReason Evaluate(DecayExecutionResult executionResult)
        {
            if (executionResult == null) return BattleFlowDenialReason.DecayNotResolved;
            BattleFactContext current = _battleState.CurrentFactContext;
            if (current.Phase != BattlePhase.DecayProcess
                || executionResult.Context.GameNumber != current.GameNumber
                || executionResult.Context.RoundNumber != current.RoundNumber
                || executionResult.Context.Phase != current.Phase)
                return BattleFlowDenialReason.DecayResolutionIncomplete;

            if (executionResult.PairResolutions.Count != BattleRules.SlotsPerSide)
                return BattleFlowDenialReason.DecayResolutionIncomplete;

            for (int i = 0; i < executionResult.PairResolutions.Count; i++)
            {
                DecayPairResolution pair = executionResult.PairResolutions[i];
                if (!ValidateSide(pair.Enemy) || !ValidateSide(pair.Player))
                    return BattleFlowDenialReason.DecayResolutionIncomplete;
            }
            return BattleFlowDenialReason.None;
        }

        private bool ValidateSide(DecaySideResolution expected)
        {
            SlotState slot = _boardState.GetSlot(expected.SlotId);
            if (slot.Condition != expected.ConditionAfter || slot.HasDice != expected.HasDiceAfter)
                return false;
            if (expected.HasDiceAfter && slot.OccupantDiceId != expected.OccupantDiceIdAfter)
                return false;

            if (expected.OriginalDiceId.IsValid)
            {
                if (!_battleInventoryState.TryGetDice(expected.OriginalDiceId, out DiceRuntimeState dice)) return false;
                if (dice.IsDecayedForCurrentGame != expected.OriginalDiceDecayed) return false;
                if (dice.HasCurrentFace != expected.OriginalDiceHasCurrentFaceAfter) return false;
                if (expected.OriginalDiceHasCurrentFaceAfter && dice.CurrentFaceIndex != expected.OriginalDiceFaceIndexAfter) return false;
                if (expected.OriginalDiceDecayed && _battleInventoryState.IsInInventory(expected.OriginalDiceId)) return false;
            }

            if (slot.HasDice)
            {
                if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState occupant)) return false;
                if (occupant.Owner != expected.SlotId.Side || occupant.IsDecayedForCurrentGame) return false;
                if (_battleInventoryState.IsInInventory(occupant.InstanceId)) return false;
            }
            return true;
        }
    }
}
