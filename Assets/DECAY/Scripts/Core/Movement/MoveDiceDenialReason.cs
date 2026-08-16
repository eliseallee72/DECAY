namespace Decay
{
    public enum MoveDiceDenialReason
    {
        None,
        BattleAlreadyComplete,
        PhaseDoesNotAllowMovement,
        ActingSideDoesNotMatchPhase,
        DiceNotTracked,
        DiceOwnedByOtherSide,
        DiceUnavailable,
        SourceStateInvalid,
        SourceSlotUnavailable,
        RepositionRequiresBoardSource,
        InventoryNotAllowedDuringReposition,
        AlreadyAtDestination,
        DestinationSideMismatch,
        DestinationSlotUnavailable,
        DestinationDiceStateInvalid,

        // Reserved for Gates whose authoritative owners are implemented later.
        // Do not manufacture processing/tutorial/blocking state merely to use these.
        ProcessingBlocksMovement,
        TutorialRestriction,
        BlockingOperationInProgress
    }
}
