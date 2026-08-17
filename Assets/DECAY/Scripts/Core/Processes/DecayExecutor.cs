using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns the ordered logical DECAY process. Pair rules are calculated by DecayResolver; this executor
    /// commits approved Commands/Facts in pair order 1..6 and owns process-local WILLSAVE queues.
    /// </summary>
    public sealed class DecayExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BattleHistory _history;
        private readonly DecayResolver _resolver;
        private readonly DecayCompletionGate _completionGate;

        public DecayExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            BattleHistory history)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _resolver = new DecayResolver(_boardState, _battleInventoryState);
            _completionGate = new DecayCompletionGate(_battleState, _boardState, _battleInventoryState);
        }

        internal DecayExecutionResult ExecuteDecay()
        {
            if (_battleState.CurrentPhase != BattlePhase.DecayProcess)
                throw new InvalidOperationException($"DECAY requires phase {BattlePhase.DecayProcess}; current phase is {_battleState.CurrentPhase}.");

            ValidateWholeBoardBeforeCommit();
            int firstFactIndex = _history.Count;
            var processState = new DecayProcessState(_battleState.CurrentFactContext);
            var pairResolutions = new List<DecayPairResolution>(BattleRules.SlotsPerSide);

            while (!processState.IsComplete)
            {
                pairResolutions.Add(ResolveNextPair(processState));
            }

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++) facts.Add(_history.Facts[i]);
            return new DecayExecutionResult(_battleState.CurrentFactContext, pairResolutions.AsReadOnly(), facts.AsReadOnly());
        }

        internal BattleFlowDenialReason EvaluateCompletion(DecayExecutionResult executionResult)
        {
            return _completionGate.Evaluate(executionResult);
        }

        private DecayPairResolution ResolveNextPair(DecayProcessState processState)
        {
            RequireCurrentProcessState(processState);
            SlotPairId pairId = processState.CurrentPairId;
            DecaySaveToken? enemySave = processState.TryPeekNextSave(Side.Enemy, out DecaySaveToken e) ? e : (DecaySaveToken?)null;
            DecaySaveToken? playerSave = processState.TryPeekNextSave(Side.Player, out DecaySaveToken p) ? p : (DecaySaveToken?)null;
            DecayPairDecision decision = _resolver.ResolvePair(pairId, enemySave, playerSave);

            // Enemy then Player is only the deterministic commit/Fact tie-break inside one simultaneous pair.
            // The resolver approved both side outcomes from the same pre-commit snapshot first.
            if (decision.MarkEnemyUnstableBefore) MarkSlotUnstable(pairId.EnemySlot);
            if (decision.MarkPlayerUnstableBefore) MarkSlotUnstable(pairId.PlayerSlot);

            ApplySideOutcome(processState, decision.Enemy);
            ApplySideOutcome(processState, decision.Player);

            if (decision.MarkEnemyUnstableAfter) MarkSlotUnstable(pairId.EnemySlot);
            if (decision.MarkPlayerUnstableAfter) MarkSlotUnstable(pairId.PlayerSlot);

            if (decision.CreateEnemySave) CreateSave(processState, decision.Enemy.Snapshot);
            if (decision.CreatePlayerSave) CreateSave(processState, decision.Player.Snapshot);

            DecayPairResolution resolution = new DecayPairResolution(
                pairId,
                CaptureSideResolution(decision.Enemy.Snapshot),
                CaptureSideResolution(decision.Player.Snapshot));
            processState.AdvancePair();
            return resolution;
        }

        private void ApplySideOutcome(DecayProcessState processState, DecaySideDecision decision)
        {
            if (decision.Outcome == DecayOutcome.Saved)
            {
                if (!decision.SaveUsed.HasValue) throw new InvalidOperationException("SAVED outcome is missing its WILLSAVE source.");
                DecaySaveToken token = processState.ConsumeNextSave(decision.Snapshot.SlotId.Side, decision.SaveUsed.Value);
                _history.Record(new SaveSpentFact(
                    _battleState.CurrentFactContext,
                    token.SourceDiceId,
                    token.SourceSlotId,
                    decision.Snapshot.DiceId,
                    decision.Snapshot.SlotId));
                SlotConditionChangedFact conditionFact = new SetSlotConditionCommand(
                    _battleState, _boardState, decision.Snapshot.SlotId, SlotCondition.Unstable).Execute();
                _history.Record(conditionFact);
                _history.Record(new DiceSavedFact(
                    _battleState.CurrentFactContext,
                    decision.Snapshot.DiceId,
                    decision.Snapshot.SlotId,
                    token.SourceDiceId,
                    token.SourceSlotId,
                    decision.Snapshot.RollValue));
            }
            else if (decision.Outcome == DecayOutcome.Decayed)
            {
                DecayDiceCommandResult result = new DecayDiceCommand(
                    _battleState,
                    _boardState,
                    _battleInventoryState,
                    decision.Snapshot.DiceId,
                    decision.Snapshot.SlotId,
                    decision.IsWillDecay,
                    decision.IsTargeted).Execute();
                _history.Record(result.DiceDecayedFact);
                _history.Record(result.SlotConditionFact);
            }
        }

        private void MarkSlotUnstable(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (slot.Condition != SlotCondition.Unbroken)
                throw new InvalidOperationException($"Only an Unbroken slot may newly become Unstable; {slotId} is {slot.Condition}.");
            _history.Record(new SetSlotConditionCommand(_battleState, _boardState, slotId, SlotCondition.Unstable).Execute());
        }

        private void CreateSave(DecayProcessState processState, DecaySideSnapshot snapshot)
        {
            SlotState slot = _boardState.GetSlot(snapshot.SlotId);
            if (!slot.HasDice || slot.OccupantDiceId != snapshot.DiceId)
                throw new InvalidOperationException($"A WILLSAVE source must still occupy {snapshot.SlotId} after its pair resolves.");
            DiceRuntimeState dice = _battleInventoryState.GetDice(snapshot.DiceId);
            if (!dice.HasCurrentFace || dice.ActiveRollValue != BattleRules.MinimumRollValue)
                throw new InvalidOperationException("A WILLSAVE source must still have roll value 1 after its pair resolves.");

            var token = new DecaySaveToken(snapshot.DiceId, snapshot.SlotId);
            processState.AddSave(token);
            _history.Record(new SaveCreatedFact(_battleState.CurrentFactContext, snapshot.DiceId, snapshot.SlotId));
        }

        private DecaySideResolution CaptureSideResolution(DecaySideSnapshot original)
        {
            SlotState slot = _boardState.GetSlot(original.SlotId);
            bool decayed = false;
            bool hasCurrentFace = false;
            int currentFaceIndex = 0;
            if (original.DiceId.IsValid && _battleInventoryState.TryGetDice(original.DiceId, out DiceRuntimeState dice))
            {
                decayed = dice.IsDecayedForCurrentGame;
                hasCurrentFace = dice.HasCurrentFace;
                currentFaceIndex = dice.CurrentFaceIndex;
            }
            return new DecaySideResolution(
                original.SlotId,
                slot.Condition,
                slot.HasDice,
                slot.HasDice ? slot.OccupantDiceId : default,
                original.DiceId,
                decayed,
                hasCurrentFace,
                currentFaceIndex);
        }

        private void ValidateWholeBoardBeforeCommit()
        {
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ValidateSlot(new SlotId(Side.Enemy, number));
                ValidateSlot(new SlotId(Side.Player, number));
            }
        }

        private void ValidateSlot(SlotId slotId)
        {
            SlotState slot = _boardState.GetSlot(slotId);
            if (!slot.HasDice) return;
            if (slot.Condition == SlotCondition.Broken)
                throw new InvalidOperationException($"Broken slot {slotId} cannot contain dice.");
            if (!_battleInventoryState.TryGetDice(slot.OccupantDiceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Board slot {slotId} contains untracked dice {slot.OccupantDiceId}.");
            if (dice.Owner != slotId.Side)
                throw new InvalidOperationException($"Dice {dice.InstanceId} ownership does not match slot {slotId}.");
            if (_battleInventoryState.IsInInventory(dice.InstanceId))
                throw new InvalidOperationException($"Dice {dice.InstanceId} cannot be both on Board and in Battle Inventory.");
            if (dice.IsDecayedForCurrentGame)
                throw new InvalidOperationException($"DECAYED dice {dice.InstanceId} cannot still occupy the Board.");
            if (!dice.HasCurrentFace)
                throw new InvalidOperationException($"Dice {dice.InstanceId} has no current rolled face before DECAY begins.");
        }

        private void RequireCurrentProcessState(DecayProcessState processState)
        {
            if (processState == null) throw new ArgumentNullException(nameof(processState));
            BattleFactContext current = _battleState.CurrentFactContext;
            if (processState.Context != current)
                throw new InvalidOperationException("DECAY process state does not belong to the active game/round/phase.");
            if (processState.IsComplete)
                throw new InvalidOperationException("DECAY process state is already complete.");
        }
    }
}
