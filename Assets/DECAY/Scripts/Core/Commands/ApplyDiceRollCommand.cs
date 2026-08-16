using System;

namespace Decay
{
    /// <summary>
    /// Applies one already-resolved face result to one dice. Random selection and phase permission
    /// remain outside this Command so the same operation can later support Gamble rerolls.
    /// </summary>
    public sealed class ApplyDiceRollCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly DiceRuntimeState _diceState;
        private readonly SlotId _slotId;
        private readonly int _selectedFaceIndex;
        private bool _wasExecuted;

        internal ApplyDiceRollCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            DiceRuntimeState diceState,
            SlotId slotId,
            int selectedFaceIndex)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _diceState = diceState ?? throw new ArgumentNullException(nameof(diceState));

            if (!slotId.IsValid)
            {
                throw new ArgumentException("A valid slot ID is required.", nameof(slotId));
            }

            if (!diceState.TryGetFace(selectedFaceIndex, out _))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(selectedFaceIndex),
                    selectedFaceIndex,
                    "The selected face index is not present on this dice.");
            }

            _slotId = slotId;
            _selectedFaceIndex = selectedFaceIndex;
        }

        internal DiceRolledFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("An ApplyDiceRollCommand can only execute once.");
            }

            SlotState slot = _boardState.GetSlot(_slotId);
            if (slot.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException($"Dice can only roll in an Unbroken slot; {_slotId} is {slot.Condition}.");
            }

            if (!slot.HasDice || slot.OccupantDiceId != _diceState.InstanceId)
            {
                throw new InvalidOperationException(
                    $"Slot {_slotId} does not currently contain dice {_diceState.InstanceId}.");
            }

            if (!_battleInventoryState.TryGetDice(_diceState.InstanceId, out DiceRuntimeState trackedDice)
                || !ReferenceEquals(trackedDice, _diceState))
            {
                throw new InvalidOperationException(
                    $"Dice {_diceState.InstanceId} is not the authoritative runtime dice tracked for this battle.");
            }

            if (_battleInventoryState.IsInInventory(_diceState.InstanceId))
            {
                throw new InvalidOperationException(
                    $"Dice {_diceState.InstanceId} cannot be both on the Board and in the current Battle Inventory.");
            }

            if (_diceState.Owner != _slotId.Side)
            {
                throw new InvalidOperationException(
                    $"Dice {_diceState.InstanceId} belongs to {_diceState.Owner} but is in {_slotId}.");
            }

            if (_diceState.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException($"DECAYED dice {_diceState.InstanceId} cannot be rolled this game.");
            }

            if (!_diceState.TryGetFace(_selectedFaceIndex, out DiceFaceRuntimeState selectedFace))
            {
                throw new InvalidOperationException(
                    $"Selected face {_selectedFaceIndex} is no longer present on dice {_diceState.InstanceId}.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            _diceState.SetCurrentFace(_selectedFaceIndex);
            _wasExecuted = true;

            return new DiceRolledFact(
                context,
                _diceState.InstanceId,
                _diceState.Owner,
                _slotId,
                _selectedFaceIndex,
                selectedFace.RollValue);
        }
    }
}
