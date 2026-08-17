namespace Decay
{
    public enum PhaseChangeDenialReason
    {
        None,
        TransitionNotAllowed,
        RoundLimitRequiresGameEnd,
        MoreGamesRemain,
        FinalGameRequiresBattleEnd,
        BattleAlreadyComplete,
        BoardBreakRequiresGameEnd,
        GameEndConditionNotMet
    }
}
