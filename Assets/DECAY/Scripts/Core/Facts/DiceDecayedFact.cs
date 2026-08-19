namespace Decay
{
    /// <summary>
    /// Historical record that one dice completed DECAY and left the Board for the remainder of the game.
    /// WasWillDecay identifies an own rolled 6; WasTargeted identifies an opposing rolled 6. When targeted,
    /// TargetingDiceId/TargetingSlotId preserve that opposing source from the simultaneous pair snapshot.
    /// </summary>
    public sealed class DiceDecayedFact : BattleFact
    {
        internal DiceDecayedFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            SlotId slotId,
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
        public int RollValue { get; }
        public bool WasWillDecay { get; }
        public bool WasTargeted { get; }
        public bool HasTargetingDice => TargetingDiceId.IsValid && TargetingSlotId.IsValid;
        public DiceInstanceId TargetingDiceId { get; }
        public SlotId TargetingSlotId { get; }
    }
}
