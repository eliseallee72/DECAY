using System;

namespace Decay
{
    public sealed class SetSlotConditionCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly SlotId _slotId;
        private readonly SlotCondition _condition;
        private bool _wasExecuted;

        internal SetSlotConditionCommand(
            BattleState battleState,
            BoardState boardState,
            SlotId slotId,
            SlotCondition condition)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _slotId = slotId;
            _condition = condition;
        }

        internal SlotConditionChangedFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A SetSlotConditionCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            SlotState slot = _boardState.GetSlot(_slotId);
            SlotCondition previousCondition = slot.Condition;
            if (previousCondition == _condition)
            {
                throw new InvalidOperationException($"Slot {_slotId} is already {_condition}; no state change would occur.");
            }

            _boardState.SetSlotCondition(_slotId, _condition);
            _wasExecuted = true;
            return new SlotConditionChangedFact(context, _slotId, previousCondition, slot.Condition);
        }
    }
}
