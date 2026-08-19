namespace Decay
{
    public sealed class SlotConditionChangedFact : BattleFact
    {
        internal SlotConditionChangedFact(
            BattleFactContext context,
            SlotId slotId,
            SlotCondition previousCondition,
            SlotCondition currentCondition)
        {
            Context = context;
            SlotId = slotId;
            PreviousCondition = previousCondition;
            CurrentCondition = currentCondition;
        }

        public BattleFactContext Context { get; }
        public SlotId SlotId { get; }
        public SlotCondition PreviousCondition { get; }
        public SlotCondition CurrentCondition { get; }
    }
}
