using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable completion receipt for one full logical DECAY pass. It snapshots final per-pair
    /// authority so leaving DecayProcess can be gated without turning the receipt into gameplay state.
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
            SlotCondition conditionAfter,
            bool hasDiceAfter,
            DiceInstanceId occupantDiceIdAfter,
            DiceInstanceId originalDiceId,
            bool originalDiceDecayed,
            bool originalDiceHasCurrentFaceAfter,
            int originalDiceFaceIndexAfter)
        {
            SlotId = slotId;
            ConditionAfter = conditionAfter;
            HasDiceAfter = hasDiceAfter;
            OccupantDiceIdAfter = occupantDiceIdAfter;
            OriginalDiceId = originalDiceId;
            OriginalDiceDecayed = originalDiceDecayed;
            OriginalDiceHasCurrentFaceAfter = originalDiceHasCurrentFaceAfter;
            OriginalDiceFaceIndexAfter = originalDiceFaceIndexAfter;
        }
        internal SlotId SlotId { get; }
        internal SlotCondition ConditionAfter { get; }
        internal bool HasDiceAfter { get; }
        internal DiceInstanceId OccupantDiceIdAfter { get; }
        internal DiceInstanceId OriginalDiceId { get; }
        internal bool OriginalDiceDecayed { get; }
        internal bool OriginalDiceHasCurrentFaceAfter { get; }
        internal int OriginalDiceFaceIndexAfter { get; }
    }
}
