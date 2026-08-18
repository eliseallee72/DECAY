using System;

namespace Decay
{
    /// <summary>
    /// Describes where a movement request wants a dice to end up.
    /// This is request data only; it is not authoritative dice-location state.
    /// </summary>
    public readonly struct MoveDiceTarget : IEquatable<MoveDiceTarget>
    {
        private MoveDiceTarget(MoveDiceTargetKind kind, SlotId boardSlot)
        {
            Kind = kind;
            BoardSlot = boardSlot;
        }

        public MoveDiceTargetKind Kind { get; }
        public SlotId BoardSlot { get; }

        public bool IsValid => Kind == MoveDiceTargetKind.BattleInventory
                               || (Kind == MoveDiceTargetKind.BoardSlot && BoardSlot.IsValid);

        public static MoveDiceTarget BattleInventory => new MoveDiceTarget(MoveDiceTargetKind.BattleInventory, default);

        public static MoveDiceTarget Board(SlotId slotId)
        {
            if (!slotId.IsValid)
            {
                throw new ArgumentException("A valid board slot is required.", nameof(slotId));
            }

            return new MoveDiceTarget(MoveDiceTargetKind.BoardSlot, slotId);
        }

        public bool Equals(MoveDiceTarget other)
        {
            return Kind == other.Kind && BoardSlot.Equals(other.BoardSlot);
        }

        public override bool Equals(object obj) => obj is MoveDiceTarget other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ BoardSlot.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (Kind == MoveDiceTargetKind.BattleInventory)
            {
                return "BattleInventory";
            }

            return Kind == MoveDiceTargetKind.BoardSlot ? BoardSlot.ToString() : "None";
        }

        public static bool operator ==(MoveDiceTarget left, MoveDiceTarget right) => left.Equals(right);
        public static bool operator !=(MoveDiceTarget left, MoveDiceTarget right) => !left.Equals(right);
    }
}
