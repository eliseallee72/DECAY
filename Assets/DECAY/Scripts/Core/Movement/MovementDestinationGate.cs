namespace Decay
{
    /// <summary>
    /// Answers whether the requested destination is legal for the resolved source.
    /// It reads authoritative membership/slot state but does not choose or execute a Command.
    /// </summary>
    internal sealed class MovementDestinationGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            if (context.Request.Target.Kind == MoveDiceTargetKind.BattleInventory)
            {
                return context.IsInInventory
                    ? MoveDiceDenialReason.AlreadyAtDestination
                    : MoveDiceDenialReason.None;
            }

            SlotId destinationId = context.Request.Target.BoardSlot;
            SlotState destination = context.DestinationSlot;

            if (destinationId.Side != context.Request.ActingSide)
            {
                return MoveDiceDenialReason.DestinationSideMismatch;
            }

            if (context.IsOnBoard && context.SourceSlot == destinationId)
            {
                return MoveDiceDenialReason.AlreadyAtDestination;
            }

            if (destination.Condition != SlotCondition.Unbroken)
            {
                return MoveDiceDenialReason.DestinationSlotUnavailable;
            }

            if (!destination.HasDice)
            {
                return MoveDiceDenialReason.None;
            }

            DiceInstanceId destinationDiceId = destination.OccupantDiceId;
            if (!context.BattleInventoryState.TryGetDice(destinationDiceId, out DiceRuntimeState destinationDice))
            {
                return MoveDiceDenialReason.DestinationDiceStateInvalid;
            }

            if (destinationDice.Owner != context.Request.ActingSide)
            {
                return MoveDiceDenialReason.DestinationSideMismatch;
            }

            if (destinationDice.IsDecayedForCurrentGame
                || context.BattleInventoryState.IsInInventory(destinationDiceId))
            {
                return MoveDiceDenialReason.DestinationDiceStateInvalid;
            }

            return MoveDiceDenialReason.None;
        }
    }
}
