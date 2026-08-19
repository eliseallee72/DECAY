using System;

namespace Decay
{
    /// <summary>
    /// Sole authoritative owner of battle scoring totals. Presentation such as the Abacus reads this state only.
    /// Round score is accumulated during ScoreProcess, game score is finalized at RoundEnd, and battle score is
    /// finalized at GameEnd.
    /// </summary>
    public sealed class ScoreState
    {
        private int _enemyRoundScore;
        private int _playerRoundScore;
        private int _enemyGameScore;
        private int _playerGameScore;
        private int _enemyBattleScore;
        private int _playerBattleScore;

        public int GetRoundScore(Side side) => side == Side.Enemy ? _enemyRoundScore : RequirePlayer(side, _playerRoundScore);
        public int GetGameScore(Side side) => side == Side.Enemy ? _enemyGameScore : RequirePlayer(side, _playerGameScore);
        public int GetBattleScore(Side side) => side == Side.Enemy ? _enemyBattleScore : RequirePlayer(side, _playerBattleScore);
        public int GetTotalScore(Side side)
        {
            RequireValidSide(side);
            return checked(GetBattleScore(side) + GetGameScore(side) + GetRoundScore(side));
        }

        public BattleOutcome GetCurrentBattleOutcome()
        {
            if (_playerBattleScore > _enemyBattleScore) return BattleOutcome.PlayerWin;
            if (_enemyBattleScore > _playerBattleScore) return BattleOutcome.EnemyWin;
            return BattleOutcome.Draw;
        }

        internal ScoreContributionCompletion ApplyContribution(Side side, int amount)
        {
            RequireValidSide(side);
            int currentRound = side == Side.Enemy ? _enemyRoundScore : _playerRoundScore;
            int currentGame = side == Side.Enemy ? _enemyGameScore : _playerGameScore;
            int currentBattle = side == Side.Enemy ? _enemyBattleScore : _playerBattleScore;

            // Validate every value this command will expose before mutating authority. This prevents a derived
            // TOTAL SCORE overflow from leaving Round Score partially committed without its corresponding Fact.
            int nextRound = checked(currentRound + amount);
            int nextTotal = checked(checked(currentBattle + currentGame) + nextRound);

            if (side == Side.Enemy) _enemyRoundScore = nextRound;
            else _playerRoundScore = nextRound;
            return new ScoreContributionCompletion(nextRound, nextTotal);
        }

        internal void RequireCanFinalizeRound()
        {
            checked
            {
                _ = _enemyGameScore + _enemyRoundScore;
                _ = _playerGameScore + _playerRoundScore;
            }
        }

        internal RoundScoreCompletion FinalizeRound()
        {
            RequireCanFinalizeRound();
            int enemyRound = _enemyRoundScore;
            int playerRound = _playerRoundScore;
            int nextEnemyGame = checked(_enemyGameScore + enemyRound);
            int nextPlayerGame = checked(_playerGameScore + playerRound);
            _enemyGameScore = nextEnemyGame;
            _playerGameScore = nextPlayerGame;
            _enemyRoundScore = 0;
            _playerRoundScore = 0;
            return new RoundScoreCompletion(enemyRound, playerRound, nextEnemyGame, nextPlayerGame);
        }

        internal void RequireCanFinalizeGame()
        {
            checked
            {
                _ = _enemyBattleScore + _enemyGameScore;
                _ = _playerBattleScore + _playerGameScore;
            }
        }

        internal GameScoreCompletion FinalizeGame()
        {
            RequireCanFinalizeGame();
            int enemyGame = _enemyGameScore;
            int playerGame = _playerGameScore;
            int nextEnemyBattle = checked(_enemyBattleScore + enemyGame);
            int nextPlayerBattle = checked(_playerBattleScore + playerGame);
            _enemyBattleScore = nextEnemyBattle;
            _playerBattleScore = nextPlayerBattle;
            _enemyGameScore = 0;
            _playerGameScore = 0;
            _enemyRoundScore = 0;
            _playerRoundScore = 0;
            return new GameScoreCompletion(enemyGame, playerGame, nextEnemyBattle, nextPlayerBattle);
        }

        private static int RequirePlayer(Side side, int playerValue)
        {
            RequireValidSide(side);
            return playerValue;
        }

        private static void RequireValidSide(Side side)
        {
            if (!Enum.IsDefined(typeof(Side), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            }
        }
    }

    internal readonly struct ScoreContributionCompletion
    {
        internal ScoreContributionCompletion(int roundScore, int totalScore)
        {
            RoundScore = roundScore;
            TotalScore = totalScore;
        }

        internal int RoundScore { get; }
        internal int TotalScore { get; }
    }

    internal readonly struct RoundScoreCompletion
    {
        internal RoundScoreCompletion(int enemyRoundScore, int playerRoundScore, int enemyGameScore, int playerGameScore)
        {
            EnemyRoundScore = enemyRoundScore;
            PlayerRoundScore = playerRoundScore;
            EnemyGameScore = enemyGameScore;
            PlayerGameScore = playerGameScore;
        }

        internal int EnemyRoundScore { get; }
        internal int PlayerRoundScore { get; }
        internal int EnemyGameScore { get; }
        internal int PlayerGameScore { get; }
    }

    internal readonly struct GameScoreCompletion
    {
        internal GameScoreCompletion(int enemyGameScore, int playerGameScore, int enemyBattleScore, int playerBattleScore)
        {
            EnemyGameScore = enemyGameScore;
            PlayerGameScore = playerGameScore;
            EnemyBattleScore = enemyBattleScore;
            PlayerBattleScore = playerBattleScore;
        }

        internal int EnemyGameScore { get; }
        internal int PlayerGameScore { get; }
        internal int EnemyBattleScore { get; }
        internal int PlayerBattleScore { get; }
    }
}
