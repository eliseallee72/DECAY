using System;

namespace Decay
{
    /// <summary>
    /// Asks the movement authority to move one tracked dice to a requested destination.
    /// The authoritative source is intentionally not supplied by the caller; it is derived
    /// from BoardState and BattleInventoryState when the request is evaluated.
    /// </summary>
    public sealed class MoveDiceRequest
    {
        public MoveDiceRequest(Side actingSide, DiceInstanceId diceId, MoveDiceTarget target)
        {
            if (!Enum.IsDefined(typeof(Side), actingSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actingSide), actingSide, "Acting side must be Enemy or Player.");
            }

            if (!diceId.IsValid)
            {
                throw new ArgumentException("A valid dice instance ID is required.", nameof(diceId));
            }

            if (!target.IsValid)
            {
                throw new ArgumentException("A valid movement target is required.", nameof(target));
            }

            ActingSide = actingSide;
            DiceId = diceId;
            Target = target;
        }

        public Side ActingSide { get; }
        public DiceInstanceId DiceId { get; }
        public MoveDiceTarget Target { get; }
    }
}
