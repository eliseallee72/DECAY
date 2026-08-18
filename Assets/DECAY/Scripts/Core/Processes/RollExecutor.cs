using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Performs the logical Roll process for every occupied board slot. This executor decides and
    /// commits rule results only; authored roll presentation is a later consumer of DiceRolledFacts.
    /// Full-board Roll invocation is internal so gameplay callers cannot bypass BattleController's
    /// phase and blocking-presentation boundaries.
    /// </summary>
    public sealed class RollExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BattleHistory _history;
        private readonly DiceRollResolver _primaryRollResolver;
        private readonly DiceRollResolver _fallbackRollResolver;
        private readonly RollCompletionGate _completionGate;

        /// <summary>
        /// Runtime constructor. A fallback source is required so a recoverable authored/random-source failure
        /// can be resolved inside Rolling without leaving the battle stuck. Both sources remain injected so
        /// normal battles and tutorials retain deterministic, testable randomness policies.
        /// </summary>
        public RollExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            BattleHistory history,
            IRandomSource primaryRandomSource,
            IRandomSource fallbackRandomSource)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _primaryRollResolver = new DiceRollResolver(primaryRandomSource ?? throw new ArgumentNullException(nameof(primaryRandomSource)));
            _fallbackRollResolver = new DiceRollResolver(fallbackRandomSource ?? throw new ArgumentNullException(nameof(fallbackRandomSource)));
            _completionGate = new RollCompletionGate(_battleState, _boardState, _battleInventoryState);
        }

        /// <summary>
        /// Isolated-rule constructor retained internally for tests that intentionally exercise a source failure
        /// without recovery. Gameplay composition must use the public constructor and supply a fallback source.
        /// </summary>
        internal RollExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            BattleHistory history,
            IRandomSource randomSource)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _primaryRollResolver = new DiceRollResolver(randomSource ?? throw new ArgumentNullException(nameof(randomSource)));
            _fallbackRollResolver = null;
            _completionGate = new RollCompletionGate(_battleState, _boardState, _battleInventoryState);
        }

        internal RollExecutionResult ExecuteRoll()
        {
            if (_battleState.CurrentPhase != BattlePhase.Rolling)
            {
                throw new InvalidOperationException(
                    $"The full board Roll process requires phase {BattlePhase.Rolling}; current phase is {_battleState.CurrentPhase}.");
            }

            List<RollCandidate> candidates = CollectValidatedCandidates();
            bool usedFallbackRandomSource = false;
            List<RollPlanEntry> plan;

            try
            {
                plan = ResolvePlan(candidates, _primaryRollResolver);
            }
            catch (RecoverableRandomSourceException) when (_fallbackRollResolver != null)
            {
                // The primary plan is discarded in full. No dice have been mutated yet, so retrying the entire
                // Roll from the fallback source preserves one coherent result instead of mixing scripted and
                // fallback values within a single process.
                plan = ResolvePlan(candidates, _fallbackRollResolver);
                usedFallbackRandomSource = true;
            }

            var resolutions = new List<RollResolution>(plan.Count);
            var facts = new List<DiceRolledFact>(plan.Count);
            for (int i = 0; i < plan.Count; i++)
            {
                RollPlanEntry entry = plan[i];
                var command = new ApplyDiceRollCommand(
                    _battleState,
                    _boardState,
                    _battleInventoryState,
                    entry.DiceState,
                    entry.SlotId,
                    entry.FaceIndex);

                DiceRolledFact fact = command.Execute();
                _history.Record(fact);
                facts.Add(fact);
                resolutions.Add(new RollResolution(entry.DiceState.InstanceId, entry.SlotId, entry.FaceIndex));
            }

            return new RollExecutionResult(
                _battleState.CurrentFactContext,
                resolutions.AsReadOnly(),
                facts.AsReadOnly(),
                usedFallbackRandomSource);
        }

        internal BattleFlowDenialReason EvaluateCompletion(RollExecutionResult executionResult)
        {
            return _completionGate.Evaluate(executionResult);
        }

        private static List<RollPlanEntry> ResolvePlan(
            IReadOnlyList<RollCandidate> candidates,
            DiceRollResolver resolver)
        {
            var plan = new List<RollPlanEntry>(candidates.Count);

            // Resolve every random result before mutating authoritative dice state. A recoverable source failure
            // therefore leaves no partially committed dice and can safely retry the complete plan with fallback.
            for (int i = 0; i < candidates.Count; i++)
            {
                RollCandidate candidate = candidates[i];
                int faceIndex = resolver.ResolveFaceIndex(candidate.DiceState);
                plan.Add(new RollPlanEntry(candidate.SlotId, candidate.DiceState, faceIndex));
            }

            return plan;
        }

        private List<RollCandidate> CollectValidatedCandidates()
        {
            var candidates = new List<RollCandidate>();

            // This is Roll-specific random-consumption order, not a universal slot-resolution rule.
            // Each board column is considered from 1 through 6; Enemy then Player is a deterministic
            // tie-break within the column so seeded/scripted sources never depend on hierarchy order.
            for (int slotNumber = 1; slotNumber <= BattleRules.SlotsPerSide; slotNumber++)
            {
                AddCandidateIfOccupied(new SlotId(Side.Enemy, slotNumber), candidates);
                AddCandidateIfOccupied(new SlotId(Side.Player, slotNumber), candidates);
            }

            return candidates;
        }

        private void AddCandidateIfOccupied(SlotId slotId, List<RollCandidate> candidates)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!slot.HasDice)
            {
                return;
            }

            if (slot.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException(
                    $"Occupied roll slot {slotId} must be Unbroken; current condition is {slot.Condition}.");
            }

            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState diceState))
            {
                throw new InvalidOperationException(
                    $"Board slot {slotId} contains dice {slot.OccupantDiceId}, but Battle Inventory does not track it.");
            }

            if (_battleInventoryState.IsInInventory(diceState.InstanceId))
            {
                throw new InvalidOperationException(
                    $"Dice {diceState.InstanceId} cannot be both in Board slot {slotId} and in the current Battle Inventory.");
            }

            if (diceState.Owner != slotId.Side)
            {
                throw new InvalidOperationException(
                    $"Dice {diceState.InstanceId} belongs to {diceState.Owner} but occupies {slotId}.");
            }

            if (diceState.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException($"DECAYED dice {diceState.InstanceId} cannot be rolled this game.");
            }

            candidates.Add(new RollCandidate(slotId, diceState));
        }

        private readonly struct RollCandidate
        {
            internal RollCandidate(SlotId slotId, DiceRuntimeState diceState)
            {
                SlotId = slotId;
                DiceState = diceState;
            }

            internal SlotId SlotId { get; }
            internal DiceRuntimeState DiceState { get; }
        }

        private readonly struct RollPlanEntry
        {
            internal RollPlanEntry(SlotId slotId, DiceRuntimeState diceState, int faceIndex)
            {
                SlotId = slotId;
                DiceState = diceState;
                FaceIndex = faceIndex;
            }

            internal SlotId SlotId { get; }
            internal DiceRuntimeState DiceState { get; }
            internal int FaceIndex { get; }
        }
    }
}
