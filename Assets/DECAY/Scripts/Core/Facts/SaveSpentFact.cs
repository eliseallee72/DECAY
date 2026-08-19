namespace Decay
{
    /// <summary>
    /// Historical record that one previously-created WILLSAVE was consumed by the first eligible
    /// later threatened dice on the same side.
    /// </summary>
    public sealed class SaveSpentFact : BattleFact
    {
        internal SaveSpentFact(
            BattleFactContext context,
            DiceInstanceId sourceDiceId,
            SlotId sourceSlotId,
            DiceInstanceId targetDiceId,
            SlotId targetSlotId)
        {
            Context = context;
            SourceDiceId = sourceDiceId;
            SourceSlotId = sourceSlotId;
            TargetDiceId = targetDiceId;
            TargetSlotId = targetSlotId;
            Side = targetSlotId.Side;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId SourceDiceId { get; }
        public SlotId SourceSlotId { get; }
        public DiceInstanceId TargetDiceId { get; }
        public SlotId TargetSlotId { get; }
        public Side Side { get; }
    }
}
