using System;
using System.Collections.Generic;

namespace Decay
{
    internal sealed class RoundEndExecutionResult
    {
        private readonly IReadOnlyList<RoundEndSlotSnapshot> _slots;
        private readonly IReadOnlyList<BattleFact> _facts;

        internal RoundEndExecutionResult(
            BattleFactContext context,
            RoundScoreCompletion scoreCompletion,
            bool gameEndRequired,
            IReadOnlyList<RoundEndSlotSnapshot> slots,
            IReadOnlyList<BattleFact> facts)
        {
            if (context.Phase != BattlePhase.RoundEnd)
                throw new ArgumentException("RoundEnd result must belong to RoundEnd.", nameof(context));
            if (slots == null || slots.Count != BattleRules.SlotsPerSide * 2)
                throw new ArgumentException("RoundEnd result must snapshot all twelve board slots.", nameof(slots));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            Context = context;
            ScoreCompletion = scoreCompletion;
            GameEndRequired = gameEndRequired;
            _slots = new List<RoundEndSlotSnapshot>(slots).AsReadOnly();
            _facts = new List<BattleFact>(facts).AsReadOnly();
        }

        internal BattleFactContext Context { get; }
        internal RoundScoreCompletion ScoreCompletion { get; }
        internal bool GameEndRequired { get; }
        internal IReadOnlyList<RoundEndSlotSnapshot> Slots => _slots;
        internal IReadOnlyList<BattleFact> Facts => _facts;
    }

    internal readonly struct RoundEndSlotSnapshot
    {
        internal RoundEndSlotSnapshot(SlotId slotId, SlotCondition condition, bool hasDice, DiceInstanceId occupantDiceId)
        {
            SlotId = slotId;
            Condition = condition;
            HasDice = hasDice;
            OccupantDiceId = occupantDiceId;
        }
        internal SlotId SlotId { get; }
        internal SlotCondition Condition { get; }
        internal bool HasDice { get; }
        internal DiceInstanceId OccupantDiceId { get; }
    }
}
