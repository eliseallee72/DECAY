namespace Decay
{
    public sealed class PhaseChangedFact : BattleFact
    {
        internal PhaseChangedFact(BattleFactContext previousContext, BattleFactContext currentContext)
        {
            PreviousContext = previousContext;
            CurrentContext = currentContext;
        }

        public BattleFactContext PreviousContext { get; }
        public BattleFactContext CurrentContext { get; }
        public BattlePhase PreviousPhase => PreviousContext.Phase;
        public BattlePhase CurrentPhase => CurrentContext.Phase;
        public int GameNumber => CurrentContext.GameNumber;
        public int RoundNumber => CurrentContext.RoundNumber;
    }
}
