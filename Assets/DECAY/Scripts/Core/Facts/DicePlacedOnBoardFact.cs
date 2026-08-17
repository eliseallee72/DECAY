namespace Decay
{
    public sealed class DicePlacedOnBoardFact : BattleFact
    {
        internal DicePlacedOnBoardFact(BattleFactContext context, DiceInstanceId diceId, Side side, SlotId destinationSlot)
        {
            Context = context;
            DiceId = diceId;
            Side = side;
            DestinationSlot = destinationSlot;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public Side Side { get; }
        public SlotId DestinationSlot { get; }
    }
}
