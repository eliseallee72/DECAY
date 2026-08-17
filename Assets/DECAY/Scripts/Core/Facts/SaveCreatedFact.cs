namespace Decay
{
    /// <summary>
    /// Historical record that a surviving rolled 1 became a WILLSAVE after its slot pair resolved.
    /// The live pending-save queue belongs to DecayProcessState, not this Fact.
    /// </summary>
    public sealed class SaveCreatedFact : BattleFact
    {
        internal SaveCreatedFact(BattleFactContext context, DiceInstanceId sourceDiceId, SlotId sourceSlotId)
        {
            Context = context;
            SourceDiceId = sourceDiceId;
            SourceSlotId = sourceSlotId;
            Side = sourceSlotId.Side;
        }

        public BattleFactContext Context { get; }
        public DiceInstanceId SourceDiceId { get; }
        public SlotId SourceSlotId { get; }
        public Side Side { get; }
    }
}
