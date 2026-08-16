namespace Decay
{
    public sealed class BoardInventoryDiceSwappedFact : BattleFact
    {
        internal BoardInventoryDiceSwappedFact(
            BattleFactContext context,
            Side side,
            SlotId slotId,
            DiceInstanceId boardToInventoryDiceId,
            DiceInstanceId inventoryToBoardDiceId)
        {
            Context = context;
            Side = side;
            SlotId = slotId;
            BoardToInventoryDiceId = boardToInventoryDiceId;
            InventoryToBoardDiceId = inventoryToBoardDiceId;
        }

        public BattleFactContext Context { get; }
        public Side Side { get; }
        public SlotId SlotId { get; }
        public DiceInstanceId BoardToInventoryDiceId { get; }
        public DiceInstanceId InventoryToBoardDiceId { get; }
    }
}
