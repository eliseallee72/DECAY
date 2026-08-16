namespace Decay
{
    public enum BattleFlowDenialReason
    {
        None = 0,
        BattleAlreadyComplete = 1,
        WrongPhase = 2,
        EnemySetupMustComplete = 3,
        EnemySetupAlreadyComplete = 4,
        RollNotResolved = 5
    }
}
