using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns committed DECAY execution. Ordered rule calculation and process-local WILLSAVE sequencing are
    /// shared with predictive preview through DecayProcessResolver; this executor alone commits Commands/Facts.
    /// </summary>
    public sealed class DecayExecutor
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BattleHistory _history;
        private readonly DecayResolver _resolver;
        private readonly DecayProcessResolver _processResolver;
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
            _processResolver = new DecayProcessResolver(_resolver);
            _completionGate = new DecayCompletionGate(_battleState, _boardState, _battleInventoryState);
        }

        internal DecayExecutionResult ExecuteDecay()
        {
            if (_battleState.CurrentPhase != BattlePhase.DecayProcess)
                throw new InvalidOperationException($"DECAY requires phase {BattlePhase.DecayProcess}; current phase is {_battleState.CurrentPhase}.");

            ValidateWholeBoardBeforeCommit();
            int firstFactIndex = _history.Count;
            var processState = new DecayProcessState();
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

        internal DecayPreviewResult ResolvePreview()
        {
            ValidateWholeBoardBeforeCommit();
            var processState = new DecayProcessState();
            var pairs = new List<DecayPreviewPair>(BattleRules.SlotsPerSide);
            while (!processState.IsComplete)
            {
                pairs.Add(new DecayPreviewPair(_processResolver.ResolveNext(processState)));
            }
            return new DecayPreviewResult(pairs.AsReadOnly());
        }

        private DecayPairResolution ResolveNextPair(DecayProcessState processState)
        {
            RequireCurrentProcessState(processState);
            DecayPairDecision decision = _processResolver.ResolveNext(processState);
            SlotPairId pairId = decision.PairId;

            // Enemy then Player is only the deterministic commit/Fact tie-break inside one simultaneous pair.
            // Both side outcomes were approved from the same pre-commit pair snapshot.
            if (decision.MarkEnemyUnstableBefore) MarkSlotUnstable(pairId.EnemySlot);
            if (decision.MarkPlayerUnstableBefore) MarkSlotUnstable(pairId.PlayerSlot);

            ApplySideOutcome(decision.Enemy);
            ApplySideOutcome(decision.Player);

            if (decision.MarkEnemyUnstableAfter) MarkSlotUnstable(pairId.EnemySlot);
            if (decision.MarkPlayerUnstableAfter) MarkSlotUnstable(pairId.PlayerSlot);

            if (decision.CreateEnemySave) RecordCreatedSave(decision.Enemy.Snapshot);
            if (decision.CreatePlayerSave) RecordCreatedSave(decision.Player.Snapshot);

            return new DecayPairResolution(
                pairId,
                CaptureSideResolution(decision.Enemy, decision.CreateEnemySave),
                CaptureSideResolution(decision.Player, decision.CreatePlayerSave));
        }

        private void ApplySideOutcome(DecaySideDecision decision)
        {
            if (decision.Outcome == DecayOutcome.Saved)
            {
                if (!decision.SaveUsed.HasValue) throw new InvalidOperationException("SAVED outcome is missing its WILLSAVE source.");
                DecaySaveToken token = decision.SaveUsed.Value;
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
                    decision.Snapshot.RollValue,
                    decision.IsWillDecay,
                    decision.IsTargeted,
                    decision.TargetingDiceId,
                    decision.TargetingSlotId));
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
                    decision.IsTargeted,
                    decision.TargetingDiceId,
                    decision.TargetingSlotId).Execute();
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

        private void RecordCreatedSave(DecaySideSnapshot snapshot)
        {
            SlotState slot = _boardState.GetSlot(snapshot.SlotId);
            if (!slot.HasDice || slot.OccupantDiceId != snapshot.DiceId)
                throw new InvalidOperationException($"A WILLSAVE source must still occupy {snapshot.SlotId} after its pair resolves.");
            DiceRuntimeState dice = _battleInventoryState.GetDice(snapshot.DiceId);
            if (!dice.HasCurrentFace || dice.ActiveRollValue != BattleRules.MinimumRollValue)
                throw new InvalidOperationException("A WILLSAVE source must still have roll value 1 after its pair resolves.");

            _history.Record(new SaveCreatedFact(_battleState.CurrentFactContext, snapshot.DiceId, snapshot.SlotId));
        }

        private DecaySideResolution CaptureSideResolution(DecaySideDecision decision, bool createsSave)
        {
            DecaySideSnapshot original = decision.Snapshot;
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
                original.EffectiveCondition,
                original.HasDice,
                original.DiceId,
                original.RollValue,
                decision.IsWillDecay,
                decision.IsTargeted,
                decision.TargetingDiceId,
                decision.TargetingSlotId,
                decision.IsDecayEligible,
                decision.Outcome,
                decision.SaveUsed.HasValue,
                decision.SaveUsed.HasValue ? decision.SaveUsed.Value.SourceDiceId : default,
                decision.SaveUsed.HasValue ? decision.SaveUsed.Value.SourceSlotId : default,
                createsSave,
                slot.Condition,
                slot.HasDice,
                slot.HasDice ? slot.OccupantDiceId : default,
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
            if (processState.IsComplete)
                throw new InvalidOperationException("DECAY process state is already complete.");
        }
    }
}
