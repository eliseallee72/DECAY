using System;

namespace Decay
{
    public sealed class BoardState
    {
        private readonly SlotState[] _enemySlots = new SlotState[BattleRules.SlotsPerSide];
        private readonly SlotState[] _playerSlots = new SlotState[BattleRules.SlotsPerSide];

        public BoardState()
        {
            for (int storageIndex = 0; storageIndex < BattleRules.SlotsPerSide; storageIndex++)
            {
                _enemySlots[storageIndex] = new SlotState(SlotIndexConverter.FromStorageIndex(Side.Enemy, storageIndex));
                _playerSlots[storageIndex] = new SlotState(SlotIndexConverter.FromStorageIndex(Side.Player, storageIndex));
            }
        }

        public SlotState GetSlot(SlotId slotId)
        {
            RequireValidSlot(slotId);
            return GetSlots(slotId.Side)[SlotIndexConverter.ToStorageIndex(slotId)];
        }

        public bool IsDiceOnBoard(DiceInstanceId diceId)
        {
            return TryGetSlotOfDice(diceId, out _);
        }

        public bool TryGetSlotOfDice(DiceInstanceId diceId, out SlotId slotId)
        {
            if (!diceId.IsValid)
            {
                slotId = default;
                return false;
            }

            if (TryFindDiceInSlots(_enemySlots, diceId, out slotId))
            {
                return true;
            }

            return TryFindDiceInSlots(_playerSlots, diceId, out slotId);
        }

        public int BrokenSlotCount(Side side)
        {
            RequireValidSide(side);
            SlotState[] slots = GetSlots(side);
            int count = 0;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Condition == SlotCondition.Broken)
                {
                    count++;
                }
            }

            return count;
        }

        public bool AreAllSlotsBroken(Side side)
        {
            return BrokenSlotCount(side) == BattleRules.SlotsPerSide;
        }

        internal void PlaceDice(SlotId slotId, DiceInstanceId diceId)
        {
            RequireValidSlot(slotId);
            RequireValidDice(diceId);

            if (IsDiceOnBoard(diceId))
            {
                throw new InvalidOperationException($"Dice {diceId} is already on the board.");
            }

            GetSlot(slotId).Occupy(diceId);
        }

        internal DiceInstanceId RemoveDice(SlotId slotId)
        {
            RequireValidSlot(slotId);
            return GetSlot(slotId).Vacate();
        }

        internal void MoveDice(SlotId sourceSlotId, SlotId destinationSlotId)
        {
            RequireMatchingSides(sourceSlotId, destinationSlotId);

            SlotState source = GetSlot(sourceSlotId);
            SlotState destination = GetSlot(destinationSlotId);

            if (!source.HasDice)
            {
                throw new InvalidOperationException($"Source slot {sourceSlotId} is empty.");
            }

            if (source.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException($"Source slot {sourceSlotId} must be Unbroken for board movement.");
            }

            if (destination.HasDice)
            {
                throw new InvalidOperationException($"Destination slot {destinationSlotId} is occupied; use SwapDice for two occupied slots.");
            }

            if (destination.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException($"Destination slot {destinationSlotId} must be Unbroken.");
            }

            DiceInstanceId diceId = source.Vacate();
            destination.Occupy(diceId);
        }

        internal void SwapDice(SlotId firstSlotId, SlotId secondSlotId)
        {
            RequireMatchingSides(firstSlotId, secondSlotId);

            SlotState first = GetSlot(firstSlotId);
            SlotState second = GetSlot(secondSlotId);

            if (!first.HasDice || !second.HasDice)
            {
                throw new InvalidOperationException("Both slots must contain dice for a board swap.");
            }

            if (first.Condition != SlotCondition.Unbroken || second.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException("Both slots must be Unbroken for a board swap.");
            }

            DiceInstanceId firstDiceId = first.Vacate();
            DiceInstanceId secondDiceId = second.Vacate();
            first.Occupy(secondDiceId);
            second.Occupy(firstDiceId);
        }

        internal void SetSlotCondition(SlotId slotId, SlotCondition condition)
        {
            RequireValidSlot(slotId);
            GetSlot(slotId).SetCondition(condition);
        }

        private SlotState[] GetSlots(Side side)
        {
            return side == Side.Enemy ? _enemySlots : _playerSlots;
        }

        private static bool TryFindDiceInSlots(SlotState[] slots, DiceInstanceId diceId, out SlotId slotId)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].HasDice && slots[i].OccupantDiceId == diceId)
                {
                    slotId = slots[i].Id;
                    return true;
                }
            }

            slotId = default;
            return false;
        }

        private static void RequireMatchingSides(SlotId first, SlotId second)
        {
            RequireValidSlot(first);
            RequireValidSlot(second);

            if (first == second)
            {
                throw new InvalidOperationException("Source and destination slots must be different.");
            }

            if (first.Side != second.Side)
            {
                throw new InvalidOperationException("Board movement cannot cross between Enemy and Player sides.");
            }
        }

        private static void RequireValidSlot(SlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("A valid slot ID is required.", nameof(slotId));
            }
        }

        private static void RequireValidDice(DiceInstanceId diceId)
        {
            if (!diceId.IsValid)
            {
                throw new ArgumentException("A valid dice instance ID is required.", nameof(diceId));
            }
        }

        private static void RequireValidSide(Side side)
        {
            if (!Enum.IsDefined(typeof(Side), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            }
        }
    }
}
