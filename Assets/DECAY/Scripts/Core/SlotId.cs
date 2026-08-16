
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Decay
{
    [Serializable]
    public struct SlotId : IEquatable<SlotId>
    {
        [SerializeField] private Side _side;
        [FormerlySerializedAs("_index")]
        [SerializeField] private int _number;

        public SlotId(Side side, int number)
        {
            if (!Enum.IsDefined(typeof(Side), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            }

            if (number < BattleRules.FirstSlotNumber || number > BattleRules.LastSlotNumber)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    number,
                    $"Slot number must be between {BattleRules.FirstSlotNumber} and {BattleRules.LastSlotNumber}.");
            }

            _side = side;
            _number = number;
        }

        public Side Side => _side;
        public int Number => _number;
        public bool IsValid => Enum.IsDefined(typeof(Side), _side)
                               && _number >= BattleRules.FirstSlotNumber
                               && _number <= BattleRules.LastSlotNumber;

        public SlotId Opposing
        {
            get
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("An invalid SlotId does not have an opposing slot.");
                }

                return new SlotId(_side == Side.Enemy ? Side.Player : Side.Enemy, _number);
            }
        }

        public bool Equals(SlotId other)
        {
            return _side == other._side && _number == other._number;
        }

        public override bool Equals(object obj) => obj is SlotId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)_side * 397) ^ _number;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"{_number}{(_side == Side.Enemy ? "E" : "P")}" : "None";
        }

        public static bool operator ==(SlotId left, SlotId right) => left.Equals(right);
        public static bool operator !=(SlotId left, SlotId right) => !left.Equals(right);
    }
}
