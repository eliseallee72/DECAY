using System;

namespace Decay
{
    // Numeric values are stable serialized identities only. Battle flow must use
    // BattlePhaseTransitionValidator rather than arithmetic or enum ordering.
    public enum BattlePhase
    {
        EnemySetup = 0,
        Rolling = 1,
        EnemyReposition = 2,
        PlayerReposition = 3,
        DecayProcess = 4,
        ScoreProcess = 5,
        RoundEnd = 6,
        GameEnd = 7,
        BattleEnd = 8,
        PlayerSetup = 9,

        // Migration alias only. The old serialized Setup value (0) now means EnemySetup.
        // New code must use EnemySetup or PlayerSetup explicitly.
        [Obsolete("Use EnemySetup or PlayerSetup explicitly.")]
        Setup = EnemySetup
    }
}
