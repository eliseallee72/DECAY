namespace Decay
{
    // These values are explicit serialization identities only. Battle flow must use
    // BattlePhaseTransitionValidator rather than arithmetic or enum ordering.
    // Step 5 v2 is the clean baseline: no numeric identity is preserved for the abandoned Setup sub-turn pass.
    public enum BattlePhase
    {
        EnemySetup = 0,
        PlayerSetup = 1,
        Rolling = 2,
        EnemyReposition = 3,
        PlayerReposition = 4,
        DecayProcess = 5,
        ScoreProcess = 6,
        RoundEnd = 7,
        GameEnd = 8,
        BattleEnd = 9
    }
}
