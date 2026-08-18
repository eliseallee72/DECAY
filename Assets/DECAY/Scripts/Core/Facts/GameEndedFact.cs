namespace Decay
{
    public sealed class GameEndedFact : BattleFact
    {
        internal GameEndedFact(
            BattleFactContext context,
            int enemyGameScore,
            int playerGameScore,
            int enemyBattleScore,
            int playerBattleScore)
        {
            Context = context;
            EnemyGameScore = enemyGameScore;
            PlayerGameScore = playerGameScore;
            EnemyBattleScore = enemyBattleScore;
            PlayerBattleScore = playerBattleScore;
        }

        public BattleFactContext Context { get; }
        public int EnemyGameScore { get; }
        public int PlayerGameScore { get; }
        public int EnemyBattleScore { get; }
        public int PlayerBattleScore { get; }
    }
}
