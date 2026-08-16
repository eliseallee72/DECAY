namespace Decay
{
    public enum MoveDiceDenialReason
    {
        None = 0,
        BattleAlreadyComplete = 1,
        PhaseDoesNotAllowMovement = 2,
        ActingSideDoesNotMatchPhase = 3,
        DiceNotTracked = 4,
        DiceOwnedByOtherSide = 5,
        DiceUnavailable = 6,
        SourceStateInvalid = 7,
        SourceSlotUnavailable = 8,
        RepositionRequiresBoardSource = 9,
        InventoryNotAllowedDuringReposition = 10,
        AlreadyAtDestination = 11,
        DestinationSideMismatch = 12,
        DestinationSlotUnavailable = 13,
        DestinationDiceStateInvalid = 14,

        // Reserved for Gates whose authoritative owners are implemented later.
        // Step 3 does not manufacture processing/tutorial/blocking state merely to use these.
        ProcessingBlocksMovement = 15,
        TutorialRestriction = 16,
        BlockingOperationInProgress = 17
    }
}
