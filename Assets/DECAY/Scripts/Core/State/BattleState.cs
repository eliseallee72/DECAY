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
            CurrentPhase = BattlePhase.Setup;
            CurrentSetupTurn = BattleSetupTurn.Enemy;
        }

        public int GamesPerBattle { get; }
        public int RoundsPerGame { get; }
        public int CurrentGameNumber { get; private set; }
        public int CurrentRoundNumber { get; private set; }
        public BattlePhase CurrentPhase { get; private set; }

        /// <summary>
        /// Authoritative side order inside BattlePhase.Setup. The value is meaningful for interactive
        /// Setup permissions only while CurrentPhase is Setup. Every new round/game begins with Enemy.
        /// </summary>
        public BattleSetupTurn CurrentSetupTurn { get; private set; }

        public bool IsBattleComplete => CurrentPhase == BattlePhase.BattleEnd;
        public BattleFactContext CurrentFactContext => new BattleFactContext(CurrentGameNumber, CurrentRoundNumber, CurrentPhase);

        internal void ApplyEnemySetupCompleted()
        {
            if (CurrentPhase != BattlePhase.Setup)
            {
                throw new InvalidOperationException(
                    $"Enemy Setup can only complete during {BattlePhase.Setup}; current phase is {CurrentPhase}.");
            }

            if (CurrentSetupTurn != BattleSetupTurn.Enemy)
            {
                throw new InvalidOperationException("Enemy Setup has already completed for the current round.");
            }

            CurrentSetupTurn = BattleSetupTurn.Player;
        }

        internal void ApplyApprovedPhaseTransition(BattlePhase requestedPhase)
        {
            BattlePhase previousPhase = CurrentPhase;

            if (previousPhase == BattlePhase.RoundEnd && requestedPhase == BattlePhase.Setup)
            {
                if (CurrentRoundNumber >= RoundsPerGame)
                {
                    throw new InvalidOperationException("The configured round limit has been reached; the game must end instead of starting another round.");
                }

                CurrentRoundNumber++;
                CurrentSetupTurn = BattleSetupTurn.Enemy;
            }
            else if (previousPhase == BattlePhase.GameEnd && requestedPhase == BattlePhase.Setup)
            {
                if (CurrentGameNumber >= GamesPerBattle)
                {
                    throw new InvalidOperationException("The configured game limit has been reached; the battle must end instead of starting another game.");
                }

                CurrentGameNumber++;
                CurrentRoundNumber = 1;
                CurrentSetupTurn = BattleSetupTurn.Enemy;
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
