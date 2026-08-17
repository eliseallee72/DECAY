namespace Decay
{
    /// <summary>
    /// Historical record that a threatened dice survived DECAY because a prior WILLSAVE protected it.
    /// SAVED is an outcome of this process, not a permanent DiceRuntimeState flag. Threat source identity
    /// is captured from the simultaneous pair decision so presentation/history never reconstructs it from
    /// already-mutated Board state.
    /// </summary>
    public sealed class DiceSavedFact : BattleFact
    {
        internal DiceSavedFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            SlotId slotId,
            DiceInstanceId saviorDiceId,
            SlotId saviorSlotId,
            int rollValue,
            bool wasWillDecay,
            bool wasTargeted,
            DiceInstanceId targetingDiceId,
            SlotId targetingSlotId)
        {
            Context = context;
            DiceId = diceId;
            SlotId = slotId;
            Side = slotId.Side;
            SaviorDiceId = saviorDiceId;
            SaviorSlotId = saviorSlotId;
            RollValue = rollValue;
            WasWillDecay = wasWillDecay;
            WasTargeted = wasTargeted;
            TargetingDiceId = targetingDiceId;
            TargetingSlotId = targetingSlotId;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public SlotId SlotId { get; }
        public Side Side { get; }
        public DiceInstanceId SaviorDiceId { get; }
        public SlotId SaviorSlotId { get; }
        public int RollValue { get; }
        public bool WasWillDecay { get; }
        public bool WasTargeted { get; }
        public bool HasTargetingDice => TargetingDiceId.IsValid && TargetingSlotId.IsValid;
        public DiceInstanceId TargetingDiceId { get; }
        public SlotId TargetingSlotId { get; }
    }
}
