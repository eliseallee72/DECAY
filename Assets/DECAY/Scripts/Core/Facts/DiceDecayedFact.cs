namespace Decay
{
    /// <summary>
    /// Historical record that one dice completed DECAY and left the Board for the remainder of the game.
    /// WasWillDecay identifies an own rolled 6; WasTargeted identifies an opposing rolled 6.
    /// </summary>
    public sealed class DiceDecayedFact : BattleFact
    {
        internal DiceDecayedFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            SlotId slotId,
            int rollValue,
            bool wasWillDecay,
            bool wasTargeted)
        {
            Context = context;
            DiceId = diceId;
            SlotId = slotId;
            Side = slotId.Side;
            RollValue = rollValue;
            WasWillDecay = wasWillDecay;
            WasTargeted = wasTargeted;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public SlotId SlotId { get; }
        public Side Side { get; }
        public int RollValue { get; }
        public bool WasWillDecay { get; }
        public bool WasTargeted { get; }
    }
}
