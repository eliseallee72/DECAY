using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable read-only prediction of the currently authoritative DECAY rules. It exists for player-facing
    /// predictive presentation during Reposition and is never committed as gameplay state.
    /// </summary>
    internal sealed class DecayPreviewResult
    {
        private readonly IReadOnlyList<DecayPreviewPair> _pairs;

        internal DecayPreviewResult(IReadOnlyList<DecayPreviewPair> pairs)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            if (pairs.Count != BattleRules.SlotsPerSide)
                throw new ArgumentException($"DECAY preview must contain exactly {BattleRules.SlotsPerSide} ordered pairs.", nameof(pairs));

            var copy = new List<DecayPreviewPair>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].PairId.Number != BattleRules.FirstSlotNumber + i)
                    throw new ArgumentException("DECAY preview pairs must be ordered exactly 1 through 6.", nameof(pairs));
                copy.Add(pairs[i]);
            }
            _pairs = copy.AsReadOnly();
        }

        internal IReadOnlyList<DecayPreviewPair> Pairs => _pairs;
    }

    internal readonly struct DecayPreviewPair
    {
        internal DecayPreviewPair(DecayPairDecision decision)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            PairId = decision.PairId;
            Enemy = new DecayPreviewSide(decision.Enemy, decision.CreateEnemySave, decision.EnemyConditionAfter);
            Player = new DecayPreviewSide(decision.Player, decision.CreatePlayerSave, decision.PlayerConditionAfter);
        }

        internal SlotPairId PairId { get; }
        internal DecayPreviewSide Enemy { get; }
        internal DecayPreviewSide Player { get; }
    }

    internal readonly struct DecayPreviewSide
    {
        internal DecayPreviewSide(DecaySideDecision decision, bool willCreateSave, SlotCondition predictedConditionAfter)
        {
            SlotId = decision.Snapshot.SlotId;
            EffectiveConditionBefore = decision.Snapshot.EffectiveCondition;
            HasDice = decision.Snapshot.HasDice;
            DiceId = decision.Snapshot.DiceId;
            RollValue = decision.Snapshot.RollValue;
            IsWillDecay = decision.IsWillDecay;
            IsTargeted = decision.IsTargeted;
            TargetingDiceId = decision.TargetingDiceId;
            TargetingSlotId = decision.TargetingSlotId;
            IsDecayEligible = decision.IsDecayEligible;
            Outcome = decision.Outcome;
            WillCreateSave = willCreateSave;
            HasSaveSource = decision.SaveUsed.HasValue;
            SaveSourceDiceId = decision.SaveUsed.HasValue ? decision.SaveUsed.Value.SourceDiceId : default;
            SaveSourceSlotId = decision.SaveUsed.HasValue ? decision.SaveUsed.Value.SourceSlotId : default;
            PredictedConditionAfter = predictedConditionAfter;
        }

        internal SlotId SlotId { get; }
        internal SlotCondition EffectiveConditionBefore { get; }
        internal bool HasDice { get; }
        internal DiceInstanceId DiceId { get; }
        internal int RollValue { get; }
        internal bool IsWillDecay { get; }
        internal bool IsTargeted { get; }
        internal DiceInstanceId TargetingDiceId { get; }
        internal SlotId TargetingSlotId { get; }
        internal bool IsDecayEligible { get; }
        internal DecayOutcome Outcome { get; }
        internal bool WillCreateSave { get; }
        internal bool HasSaveSource { get; }
        internal DiceInstanceId SaveSourceDiceId { get; }
        internal SlotId SaveSourceSlotId { get; }
        internal SlotCondition PredictedConditionAfter { get; }
    }
}
