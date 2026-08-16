
using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct DiceTagId : IEquatable<DiceTagId>
    {
        [SerializeField] private string _value;

        public DiceTagId(string value)
        {
            _value = ContentIdValidator.RequireCategory(value, "tag", nameof(value));
        }

        public string Value => _value ?? string.Empty;
        public bool IsValid => ContentIdValidator.IsValidCategoryId(_value, "tag");

        public bool Equals(DiceTagId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is DiceTagId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => IsValid ? Value : "None";
        public static bool operator ==(DiceTagId left, DiceTagId right) => left.Equals(right);
        public static bool operator !=(DiceTagId left, DiceTagId right) => !left.Equals(right);
    }
}
