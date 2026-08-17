namespace Decay
{
    public sealed class RoundEndedFact : BattleFact
    {
        internal RoundEndedFact(
            BattleFactContext context,
            int enemyRoundScore,
            int playerRoundScore,
            int enemyGameScore,
            int playerGameScore,
            bool gameEndRequired)
        {
            Context = context;
            EnemyRoundScore = enemyRoundScore;
            PlayerRoundScore = playerRoundScore;
            EnemyGameScore = enemyGameScore;
            PlayerGameScore = playerGameScore;
            GameEndRequired = gameEndRequired;
        }

        public BattleFactContext Context { get; }
        public int EnemyRoundScore { get; }
        public int PlayerRoundScore { get; }
        public int EnemyGameScore { get; }
        public int PlayerGameScore { get; }
        public bool GameEndRequired { get; }
    }
}
