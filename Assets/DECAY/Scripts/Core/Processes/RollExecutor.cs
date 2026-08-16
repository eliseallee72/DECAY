using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Performs the logical Roll process for every occupied board slot. This executor decides and
    /// commits rule results only; authored roll presentation is a later consumer of DiceRolledFacts.
    /// </summary>
    public sealed class RollExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BattleHistory _history;
        private readonly DiceRollResolver _rollResolver;

        public RollExecutor(
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
            _rollResolver = new DiceRollResolver(randomSource ?? throw new ArgumentNullException(nameof(randomSource)));
        }

        public IReadOnlyList<DiceRolledFact> ExecuteRoll()
        {
            if (_battleState.CurrentPhase != BattlePhase.Rolling)
            {
                throw new InvalidOperationException(
                    $"The full board Roll process requires phase {BattlePhase.Rolling}; current phase is {_battleState.CurrentPhase}.");
            }

            List<RollCandidate> candidates = CollectValidatedCandidates();
            var plan = new List<RollPlanEntry>(candidates.Count);

            // Resolve every random result before mutating authoritative dice state. This keeps an
            // invalid scripted sequence from leaving a half-applied logical Roll process.
            for (int i = 0; i < candidates.Count; i++)
            {
                RollCandidate candidate = candidates[i];
                int faceIndex = _rollResolver.ResolveFaceIndex(candidate.DiceState);
                plan.Add(new RollPlanEntry(candidate.SlotId, candidate.DiceState, faceIndex));
            }

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
            }

            return facts.AsReadOnly();
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
            public RollCandidate(SlotId slotId, DiceRuntimeState diceState)
            {
                SlotId = slotId;
                DiceState = diceState;
            }

            public SlotId SlotId { get; }
            public DiceRuntimeState DiceState { get; }
        }

        private readonly struct RollPlanEntry
        {
            public RollPlanEntry(SlotId slotId, DiceRuntimeState diceState, int faceIndex)
            {
                SlotId = slotId;
                DiceState = diceState;
                FaceIndex = faceIndex;
            }

            public SlotId SlotId { get; }
            public DiceRuntimeState DiceState { get; }
            public int FaceIndex { get; }
        }
    }
}
