
using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct DiceId : IEquatable<DiceId>
    {
        [SerializeField] private string _value;

        public DiceId(string value)
        {
            _value = ContentIdValidator.RequireCategory(value, "dice", nameof(value));
        }

        public string Value => _value ?? string.Empty;
        public bool IsValid => ContentIdValidator.IsValidCategoryId(_value, "dice");

        public bool Equals(DiceId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is DiceId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => IsValid ? Value : "None";
        public static bool operator ==(DiceId left, DiceId right) => left.Equals(right);
        public static bool operator !=(DiceId left, DiceId right) => !left.Equals(right);
    }
}
