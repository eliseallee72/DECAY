using System;

namespace Decay
{
    /// <summary>
    /// Process-local identity for one unused WILLSAVE. The source dice and slot are retained so
    /// deterministic consumption and SAVIOR/SAVED Facts preserve causality without dice-local flags.
    /// </summary>
    internal readonly struct DecaySaveToken : IEquatable<DecaySaveToken>
    {
        internal DecaySaveToken(DiceInstanceId sourceDiceId, SlotId sourceSlotId)
        {
            if (!sourceDiceId.IsValid) throw new ArgumentException("A valid source dice ID is required.", nameof(sourceDiceId));
            if (!sourceSlotId.IsValid) throw new ArgumentException("A valid source slot ID is required.", nameof(sourceSlotId));
            SourceDiceId = sourceDiceId;
            SourceSlotId = sourceSlotId;
        }

        internal DiceInstanceId SourceDiceId { get; }
        internal SlotId SourceSlotId { get; }
        internal Side Side => SourceSlotId.Side;

        public bool Equals(DecaySaveToken other) => SourceDiceId == other.SourceDiceId && SourceSlotId == other.SourceSlotId;
        public override bool Equals(object obj) => obj is DecaySaveToken other && Equals(other);
        public override int GetHashCode() => (SourceDiceId.GetHashCode() * 397) ^ SourceSlotId.GetHashCode();
        public static bool operator ==(DecaySaveToken left, DecaySaveToken right) => left.Equals(right);
        public static bool operator !=(DecaySaveToken left, DecaySaveToken right) => !left.Equals(right);
    }
}
