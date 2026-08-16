
using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct OwnedDiceId : IEquatable<OwnedDiceId>, IComparable<OwnedDiceId>
    {
        [SerializeField] private long _value;

        public OwnedDiceId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "An owned dice ID must be positive.");
            }

            _value = value;
        }

        public long Value => _value;
        public bool IsValid => _value > 0;

        public bool Equals(OwnedDiceId other) => _value == other._value;
        public override bool Equals(object obj) => obj is OwnedDiceId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(OwnedDiceId other) => _value.CompareTo(other._value);
        public override string ToString() => IsValid ? _value.ToString() : "None";
        public static bool operator ==(OwnedDiceId left, OwnedDiceId right) => left.Equals(right);
        public static bool operator !=(OwnedDiceId left, OwnedDiceId right) => !left.Equals(right);
    }
}
