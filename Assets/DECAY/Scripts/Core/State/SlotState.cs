using System;

namespace Decay
{
    public sealed class SlotState
    {
        private DiceInstanceId _occupantDiceId;

        internal SlotState(SlotId id)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A valid slot ID is required.", nameof(id));
            }

            Id = id;
            Condition = SlotCondition.Unbroken;
        }

        public SlotId Id { get; }
        public SlotCondition Condition { get; private set; }
        public bool HasDice => _occupantDiceId.IsValid;
        public DiceInstanceId OccupantDiceId => _occupantDiceId;

        internal void Occupy(DiceInstanceId diceId)
        {
            if (!diceId.IsValid)
            {
                throw new ArgumentException("A valid dice instance ID is required.", nameof(diceId));
            }

            if (Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException($"Slot {Id} must be Unbroken before a dice can be placed into it.");
            }

            if (HasDice)
            {
                throw new InvalidOperationException($"Slot {Id} is already occupied.");
            }

            _occupantDiceId = diceId;
        }

        internal DiceInstanceId Vacate()
        {
            if (!HasDice)
            {
                throw new InvalidOperationException($"Slot {Id} is already empty.");
            }

            DiceInstanceId removedDiceId = _occupantDiceId;
            _occupantDiceId = default;
            return removedDiceId;
        }

        internal void SetCondition(SlotCondition condition)
        {
            if (!Enum.IsDefined(typeof(SlotCondition), condition))
            {
                throw new ArgumentOutOfRangeException(nameof(condition), condition, "Slot condition is not defined.");
            }

            if (condition == SlotCondition.Broken && HasDice)
            {
                throw new InvalidOperationException(
                    $"Slot {Id} cannot become Broken while it still contains dice {_occupantDiceId}. Remove or return the dice first.");
            }

            Condition = condition;
        }
    }
}
