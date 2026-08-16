using System;

namespace Decay
{
    public sealed class BattleState
    {
        public BattleState(BattleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(config));
            }

            GamesPerBattle = config.GamesPerBattle;
            RoundsPerGame = config.RoundsPerGame;
            CurrentGameNumber = 1;
            CurrentRoundNumber = 1;
            CurrentPhase = BattlePhase.EnemySetup;
        }

        public int GamesPerBattle { get; }
        public int RoundsPerGame { get; }
        public int CurrentGameNumber { get; private set; }
        public int CurrentRoundNumber { get; private set; }
        public BattlePhase CurrentPhase { get; private set; }
        public bool IsBattleComplete => CurrentPhase == BattlePhase.BattleEnd;
        public BattleFactContext CurrentFactContext => new BattleFactContext(CurrentGameNumber, CurrentRoundNumber, CurrentPhase);

        /// <summary>
        /// Compatibility-only projection for Step 5 tests/code authored before EnemySetup and PlayerSetup
        /// became explicit BattlePhase values. This is derived from CurrentPhase and is not mutable state.
        /// New runtime code must use CurrentPhase directly.
        /// </summary>
        [Obsolete("Use CurrentPhase with BattlePhase.EnemySetup or BattlePhase.PlayerSetup.")]
        public BattleSetupTurn CurrentSetupTurn
        {
            get
            {
                if (CurrentPhase == BattlePhase.EnemySetup)
                {
                    return BattleSetupTurn.Enemy;
                }

                if (CurrentPhase == BattlePhase.PlayerSetup)
                {
                    return BattleSetupTurn.Player;
                }

                throw new InvalidOperationException(
                    $"Setup turn is not defined while current phase is {CurrentPhase}.");
            }
        }

        /// <summary>
        /// Compatibility-only migration hook for tests written against the earlier Step 5 sub-turn pass.
        /// New production flow must use BattlePhaseController to request PlayerSetup.
        /// </summary>
        [Obsolete("Use BattlePhaseController to transition EnemySetup -> PlayerSetup.")]
        internal void ApplyEnemySetupCompleted()
        {
            if (CurrentPhase != BattlePhase.EnemySetup)
            {
                throw new InvalidOperationException(
                    $"Enemy Setup can only complete during {BattlePhase.EnemySetup}; current phase is {CurrentPhase}.");
            }

            CurrentPhase = BattlePhase.PlayerSetup;
        }

        internal void ApplyApprovedPhaseTransition(BattlePhase requestedPhase)
        {
            BattlePhase previousPhase = CurrentPhase;

            if (previousPhase == BattlePhase.RoundEnd && requestedPhase == BattlePhase.EnemySetup)
            {
                if (CurrentRoundNumber >= RoundsPerGame)
                {
                    throw new InvalidOperationException("The configured round limit has been reached; the game must end instead of starting another round.");
                }

                CurrentRoundNumber++;
            }
            else if (previousPhase == BattlePhase.GameEnd && requestedPhase == BattlePhase.EnemySetup)
            {
                if (CurrentGameNumber >= GamesPerBattle)
                {
                    throw new InvalidOperationException("The configured game limit has been reached; the battle must end instead of starting another game.");
                }

                CurrentGameNumber++;
                CurrentRoundNumber = 1;
            }
            else if (previousPhase == BattlePhase.GameEnd && requestedPhase == BattlePhase.BattleEnd)
            {
                if (CurrentGameNumber < GamesPerBattle)
                {
                    throw new InvalidOperationException("The battle cannot end while configured games remain.");
                }
            }

            CurrentPhase = requestedPhase;
        }
    }
}
