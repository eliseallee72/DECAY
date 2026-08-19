namespace Decay
{
    public sealed class DiceMovedOnBoardFact : BattleFact
    {
        internal DiceMovedOnBoardFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            Side side,
            SlotId sourceSlot,
            SlotId destinationSlot)
        {
            Context = context;
            DiceId = diceId;
            Side = side;
            SourceSlot = sourceSlot;
            DestinationSlot = destinationSlot;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public Side Side { get; }
        public SlotId SourceSlot { get; }
        public SlotId DestinationSlot { get; }
    }
}
