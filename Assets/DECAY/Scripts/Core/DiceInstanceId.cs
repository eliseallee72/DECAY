using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct DiceInstanceId : IEquatable<DiceInstanceId>, IComparable<DiceInstanceId>
    {
        [SerializeField] private long _value;

        public DiceInstanceId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "A dice instance ID must be positive.");
            }

            _value = value;
        }

        public long Value => _value;
        public bool IsValid => _value > 0;

        public bool Equals(DiceInstanceId other) => _value == other._value;
        public override bool Equals(object obj) => obj is DiceInstanceId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(DiceInstanceId other) => _value.CompareTo(other._value);
        public override string ToString() => IsValid ? _value.ToString() : "None";

        public static bool operator ==(DiceInstanceId left, DiceInstanceId right) => left.Equals(right);
        public static bool operator !=(DiceInstanceId left, DiceInstanceId right) => !left.Equals(right);
    }
}
