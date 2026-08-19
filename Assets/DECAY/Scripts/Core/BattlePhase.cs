namespace Decay
{
    // These values are explicit serialization identities only. Battle flow must use
    // BattlePhaseTransitionValidator rather than arithmetic or enum ordering.
    public enum BattlePhase
    {
        Setup = 0,
        Rolling = 1,
        EnemyReposition = 2,
        PlayerReposition = 3,
        DecayProcess = 4,
        ScoreProcess = 5,
        RoundEnd = 6,
        GameEnd = 7,
        BattleEnd = 8
    }
}
