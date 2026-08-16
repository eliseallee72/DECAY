namespace Decay
{
    /// <summary>
    /// Answers whether the dice currently has exactly one movable authoritative source.
    /// </summary>
    internal sealed class MovementSourceGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            if (context.DiceState.IsDecayedForCurrentGame)
            {
                return MoveDiceDenialReason.DiceUnavailable;
            }

            if (context.IsInInventory && context.IsOnBoard)
            {
                return MoveDiceDenialReason.SourceStateInvalid;
            }

            if (!context.IsInInventory && !context.IsOnBoard)
            {
                return MoveDiceDenialReason.DiceUnavailable;
            }

            if (!context.IsOnBoard)
            {
                return MoveDiceDenialReason.None;
            }

            SlotState source = context.BoardState.GetSlot(context.SourceSlot);
            if (source.Condition != SlotCondition.Unbroken)
            {
                return MoveDiceDenialReason.SourceSlotUnavailable;
            }

            return source.Id.Side == context.Request.ActingSide
                ? MoveDiceDenialReason.None
                : MoveDiceDenialReason.SourceStateInvalid;
        }
    }
}
