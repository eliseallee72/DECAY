
using System;

namespace Decay
{
    public readonly struct SlotPairId : IEquatable<SlotPairId>
    {
        public SlotPairId(int number)
        {
            if (number < BattleRules.FirstSlotNumber || number > BattleRules.LastSlotNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    number,
                    $"Slot pair number must be between {BattleRules.FirstSlotNumber} and {BattleRules.LastSlotNumber}.");
            }

            Number = number;
        }

        public int Number { get; }
        public SlotId EnemySlot => new SlotId(Side.Enemy, Number);
        public SlotId PlayerSlot => new SlotId(Side.Player, Number);

        public SlotId GetSlot(Side side)
        {
            if (!Enum.IsDefined(typeof(Side), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            }

            return new SlotId(side, Number);
        }

        public bool Equals(SlotPairId other) => Number == other.Number;
        public override bool Equals(object obj) => obj is SlotPairId other && Equals(other);
        public override int GetHashCode() => Number;
        public override string ToString() => $"{Number}E|{Number}P";
        public static bool operator ==(SlotPairId left, SlotPairId right) => left.Equals(right);
        public static bool operator !=(SlotPairId left, SlotPairId right) => !left.Equals(right);
    }
}
