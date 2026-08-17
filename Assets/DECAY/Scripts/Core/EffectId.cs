
using System;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public struct EffectId : IEquatable<EffectId>
    {
        [SerializeField] private string _value;

        public EffectId(string value)
        {
            _value = ContentIdValidator.RequireCategory(value, "effect", nameof(value));
        }

        public string Value => _value ?? string.Empty;
        public bool IsValid => ContentIdValidator.IsValidCategoryId(_value, "effect");

        public bool Equals(EffectId other) => StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is EffectId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => IsValid ? Value : "None";
        public static bool operator ==(EffectId left, EffectId right) => left.Equals(right);
        public static bool operator !=(EffectId left, EffectId right) => !left.Equals(right);
    }
}
