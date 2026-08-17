namespace Decay
{
    /// <summary>
    /// Historical record that a threatened dice survived DECAY because a prior WILLSAVE protected it.
    /// SAVED is an outcome of this process, not a permanent DiceRuntimeState flag.
    /// </summary>
    public sealed class DiceSavedFact : BattleFact
    {
        internal DiceSavedFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            SlotId slotId,
            DiceInstanceId saviorDiceId,
            SlotId saviorSlotId,
            int rollValue)
        {
            Context = context;
            DiceId = diceId;
            SlotId = slotId;
            Side = slotId.Side;
            SaviorDiceId = saviorDiceId;
            SaviorSlotId = saviorSlotId;
            RollValue = rollValue;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public SlotId SlotId { get; }
        public Side Side { get; }
        public DiceInstanceId SaviorDiceId { get; }
        public SlotId SaviorSlotId { get; }
        public int RollValue { get; }
    }
}
