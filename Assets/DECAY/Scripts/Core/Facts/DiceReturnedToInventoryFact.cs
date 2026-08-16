namespace Decay
{
    public sealed class DiceReturnedToInventoryFact : BattleFact
    {
        internal DiceReturnedToInventoryFact(BattleFactContext context, DiceInstanceId diceId, Side side, SlotId sourceSlot)
        {
            Context = context;
            DiceId = diceId;
            Side = side;
            SourceSlot = sourceSlot;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public Side Side { get; }
        public SlotId SourceSlot { get; }
    }
}
