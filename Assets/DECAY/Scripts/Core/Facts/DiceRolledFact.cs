namespace Decay
{
    /// <summary>
    /// Historical record of one completed logical roll result. FaceIndex records which face was
    /// selected; RollValue records that face's value at the moment the roll was applied.
    /// </summary>
    public sealed class DiceRolledFact : BattleFact
    {
        internal DiceRolledFact(
            BattleFactContext context,
            DiceInstanceId diceId,
            Side side,
            SlotId slotId,
            int faceIndex,
            int rollValue)
        {
            Context = context;
            DiceId = diceId;
            Side = side;
            SlotId = slotId;
            FaceIndex = faceIndex;
            RollValue = rollValue;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId DiceId { get; }
        public Side Side { get; }
        public SlotId SlotId { get; }
        public int FaceIndex { get; }
        public int RollValue { get; }
    }
}
