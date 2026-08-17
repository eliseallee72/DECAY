namespace Decay
{
    public sealed class BattleEndedFact : BattleFact
    {
        internal BattleEndedFact(BattleFactContext context, int enemyBattleScore, int playerBattleScore, BattleOutcome outcome)
        {
            Context = context;
            EnemyBattleScore = enemyBattleScore;
            PlayerBattleScore = playerBattleScore;
            Outcome = outcome;
        }

        public BattleFactContext Context { get; }
        public int EnemyBattleScore { get; }
        public int PlayerBattleScore { get; }
        public BattleOutcome Outcome { get; }
    }
}
