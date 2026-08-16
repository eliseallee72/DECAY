namespace Decay
{
    public enum PhaseChangeDenialReason
    {
        None = 0,
        TransitionNotAllowed = 1,
        RoundLimitRequiresGameEnd = 2,
        MoreGamesRemain = 3,
        FinalGameRequiresBattleEnd = 4,
        BattleAlreadyComplete = 5,
        BoardBreakRequiresGameEnd = 6,
        GameEndConditionNotMet = 7
    }
}
