namespace Decay
{
    public sealed class DiceResetForNewGameFact : BattleFact
    {
        internal DiceResetForNewGameFact(BattleFactContext context, DiceInstanceId diceId, Side side, DiceId definitionId)
        {
            Context = context;
            DiceId = diceId;
            Side = side;
            DefinitionId = definitionId;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public Side Side { get; }
        public DiceId DefinitionId { get; }
    }
}
