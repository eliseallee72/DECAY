using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable completion receipt for one full logical DECAY pass. It preserves ordered authoritative
    /// pair decisions plus final state so later presentation never has to reconstruct DECAY rules.
    /// </summary>
    internal sealed class DecayExecutionResult
    {
        private readonly IReadOnlyList<DecayPairResolution> _pairResolutions;
        private readonly IReadOnlyList<BattleFact> _facts;

        internal DecayExecutionResult(
            BattleFactContext context,
            IReadOnlyList<DecayPairResolution> pairResolutions,
            IReadOnlyList<BattleFact> facts)
        {
            if (context.Phase != BattlePhase.DecayProcess)
                throw new ArgumentException("DECAY execution result must belong to DecayProcess.", nameof(context));
            if (pairResolutions == null) throw new ArgumentNullException(nameof(pairResolutions));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (pairResolutions.Count != BattleRules.SlotsPerSide)
                throw new ArgumentException($"DECAY must resolve exactly {BattleRules.SlotsPerSide} slot pairs.", nameof(pairResolutions));

            var copy = new List<DecayPairResolution>(pairResolutions.Count);
            for (int i = 0; i < pairResolutions.Count; i++)
            {
                DecayPairResolution resolution = pairResolutions[i];
                if (resolution.PairId.Number != BattleRules.FirstSlotNumber + i)
                    throw new ArgumentException("DECAY pair resolutions must be ordered exactly 1 through 6.", nameof(pairResolutions));
                copy.Add(resolution);
            }

            Context = context;
            _pairResolutions = copy.AsReadOnly();
            _facts = new List<BattleFact>(facts).AsReadOnly();
        }

        internal BattleFactContext Context { get; }
        internal IReadOnlyList<DecayPairResolution> PairResolutions => _pairResolutions;
        internal IReadOnlyList<BattleFact> Facts => _facts;
    }

    internal readonly struct DecayPairResolution
    {
        internal DecayPairResolution(SlotPairId pairId, DecaySideResolution enemy, DecaySideResolution player)
        {
            PairId = pairId;
            Enemy = enemy;
            Player = player;
        }
        internal SlotPairId PairId { get; }
        internal DecaySideResolution Enemy { get; }
        internal DecaySideResolution Player { get; }
    }

    internal readonly struct DecaySideResolution
    {
        internal DecaySideResolution(
            SlotId slotId,
            SlotCondition effectiveConditionBefore,
            bool hadDiceBefore,
            DiceInstanceId originalDiceId,
            int originalRollValue,
            bool wasWillDecay,
            bool wasTargeted,
            DiceInstanceId targetingDiceId,
            SlotId targetingSlotId,
            bool wasDecayEligible,
            DecayOutcome outcome,
            bool usedSave,
            DiceInstanceId saveSourceDiceId,
            SlotId saveSourceSlotId,
            bool createdSave,
            SlotCondition conditionAfter,
            bool hasDiceAfter,
            DiceInstanceId occupantDiceIdAfter,
            bool originalDiceDecayed,
            bool originalDiceHasCurrentFaceAfter,
            int originalDiceFaceIndexAfter)
        {
            SlotId = slotId;
            EffectiveConditionBefore = effectiveConditionBefore;
            HadDiceBefore = hadDiceBefore;
            OriginalDiceId = originalDiceId;
            OriginalRollValue = originalRollValue;
            WasWillDecay = wasWillDecay;
            WasTargeted = wasTargeted;
            TargetingDiceId = targetingDiceId;
            TargetingSlotId = targetingSlotId;
            WasDecayEligible = wasDecayEligible;
            Outcome = outcome;
            UsedSave = usedSave;
            SaveSourceDiceId = saveSourceDiceId;
            SaveSourceSlotId = saveSourceSlotId;
            CreatedSave = createdSave;
            ConditionAfter = conditionAfter;
            HasDiceAfter = hasDiceAfter;
            OccupantDiceIdAfter = occupantDiceIdAfter;
            OriginalDiceDecayed = originalDiceDecayed;
            OriginalDiceHasCurrentFaceAfter = originalDiceHasCurrentFaceAfter;
            OriginalDiceFaceIndexAfter = originalDiceFaceIndexAfter;
        }

        internal SlotId SlotId { get; }
        internal SlotCondition EffectiveConditionBefore { get; }
        internal bool HadDiceBefore { get; }
        internal DiceInstanceId OriginalDiceId { get; }
        internal int OriginalRollValue { get; }
        internal bool WasWillDecay { get; }
        internal bool WasTargeted { get; }
        internal DiceInstanceId TargetingDiceId { get; }
        internal SlotId TargetingSlotId { get; }
        internal bool WasDecayEligible { get; }
        internal DecayOutcome Outcome { get; }
        internal bool UsedSave { get; }
        internal DiceInstanceId SaveSourceDiceId { get; }
        internal SlotId SaveSourceSlotId { get; }
        internal bool CreatedSave { get; }
        internal SlotCondition ConditionAfter { get; }
        internal bool HasDiceAfter { get; }
        internal DiceInstanceId OccupantDiceIdAfter { get; }
        internal bool OriginalDiceDecayed { get; }
        internal bool OriginalDiceHasCurrentFaceAfter { get; }
        internal int OriginalDiceFaceIndexAfter { get; }
    }
}
