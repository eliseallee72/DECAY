
using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct EffectInstanceId : IEquatable<EffectInstanceId>, IComparable<EffectInstanceId>
    {
        [SerializeField] private long _value;

        public EffectInstanceId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "An effect instance ID must be positive.");
            }

            _value = value;
        }

        public long Value => _value;
        public bool IsValid => _value > 0;

        public bool Equals(EffectInstanceId other) => _value == other._value;
        public override bool Equals(object obj) => obj is EffectInstanceId other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(EffectInstanceId other) => _value.CompareTo(other._value);
        public override string ToString() => IsValid ? _value.ToString() : "None";
        public static bool operator ==(EffectInstanceId left, EffectInstanceId right) => left.Equals(right);
        public static bool operator !=(EffectInstanceId left, EffectInstanceId right) => !left.Equals(right);
    }
}
