using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable receipt for one successfully committed logical Roll. This is process-local evidence,
    /// not a second owner of dice values: DiceRuntimeState remains authoritative for each current face.
    /// </summary>
    internal sealed class RollExecutionResult
    {
        private readonly IReadOnlyList<RollResolution> _resolutions;
        private readonly IReadOnlyList<DiceRolledFact> _facts;

        internal RollExecutionResult(
            BattleFactContext context,
            IReadOnlyList<RollResolution> resolutions,
            IReadOnlyList<DiceRolledFact> facts,
            bool usedFallbackRandomSource)
        {
            if (context.Phase != BattlePhase.Rolling)
            {
                throw new ArgumentException("A Roll execution result must belong to the Rolling phase.", nameof(context));
            }

            if (resolutions == null)
            {
                throw new ArgumentNullException(nameof(resolutions));
            }

            if (facts == null)
            {
                throw new ArgumentNullException(nameof(facts));
            }

            if (facts.Count != resolutions.Count)
            {
                throw new ArgumentException("A completed Roll must report exactly one DiceRolledFact per resolution.", nameof(facts));
            }

            var copy = new List<RollResolution>(resolutions.Count);
            var diceIds = new HashSet<DiceInstanceId>();
            var slotIds = new HashSet<SlotId>();
            for (int i = 0; i < resolutions.Count; i++)
            {
                RollResolution resolution = resolutions[i];
                if (!resolution.DiceId.IsValid || !resolution.SlotId.IsValid || resolution.FaceIndex <= 0)
                {
                    throw new ArgumentException("Every Roll resolution must identify a valid dice, slot, and face.", nameof(resolutions));
                }

                if (!diceIds.Add(resolution.DiceId))
                {
                    throw new ArgumentException($"Roll result contains duplicate dice {resolution.DiceId}.", nameof(resolutions));
                }

                if (!slotIds.Add(resolution.SlotId))
                {
                    throw new ArgumentException($"Roll result contains duplicate slot {resolution.SlotId}.", nameof(resolutions));
                }

                copy.Add(resolution);
            }

            Context = context;
            _resolutions = copy.AsReadOnly();
            _facts = new List<DiceRolledFact>(facts).AsReadOnly();
            UsedFallbackRandomSource = usedFallbackRandomSource;
        }

        internal BattleFactContext Context { get; }
        internal IReadOnlyList<RollResolution> Resolutions => _resolutions;
        internal IReadOnlyList<DiceRolledFact> Facts => _facts;
        internal bool UsedFallbackRandomSource { get; }

        internal bool TryGetResolution(
            SlotId slotId,
            DiceInstanceId diceId,
            out RollResolution resolution)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                RollResolution candidate = _resolutions[i];
                if (candidate.SlotId == slotId && candidate.DiceId == diceId)
                {
                    resolution = candidate;
                    return true;
                }
            }

            resolution = default;
            return false;
        }
    }

    internal readonly struct RollResolution
    {
        internal RollResolution(DiceInstanceId diceId, SlotId slotId, int faceIndex)
        {
            DiceId = diceId;
            SlotId = slotId;
            FaceIndex = faceIndex;
        }

        internal DiceInstanceId DiceId { get; }
        internal SlotId SlotId { get; }
        internal int FaceIndex { get; }
    }
}
