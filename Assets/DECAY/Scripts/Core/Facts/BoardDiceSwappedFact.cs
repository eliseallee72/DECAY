namespace Decay
{
    public sealed class BoardDiceSwappedFact : BattleFact
    {
        internal BoardDiceSwappedFact(
            BattleFactContext context,
            Side side,
            DiceInstanceId firstDiceId,
            SlotId firstSourceSlot,
            SlotId firstDestinationSlot,
            DiceInstanceId secondDiceId,
            SlotId secondSourceSlot,
            SlotId secondDestinationSlot)
        {
            Context = context;
            Side = side;
            FirstDiceId = firstDiceId;
            FirstSourceSlot = firstSourceSlot;
            FirstDestinationSlot = firstDestinationSlot;
            SecondDiceId = secondDiceId;
            SecondSourceSlot = secondSourceSlot;
            SecondDestinationSlot = secondDestinationSlot;
        }

        public BattleFactContext Context { get; }
        public Side Side { get; }
        public DiceInstanceId FirstDiceId { get; }
        public SlotId FirstSourceSlot { get; }
        public SlotId FirstDestinationSlot { get; }
        public DiceInstanceId SecondDiceId { get; }
        public SlotId SecondSourceSlot { get; }
        public SlotId SecondDestinationSlot { get; }
    }
}
