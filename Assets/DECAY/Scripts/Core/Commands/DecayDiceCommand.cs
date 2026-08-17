using System;

namespace Decay
{
    /// <summary>
    /// Commits one already-approved DECAY outcome. The dice is removed from Board occupancy, marked
    /// DECAYED for the current game, and then its now-empty slot becomes Broken. It remains tracked by
    /// BattleInventoryState but is not returned to current Battle Inventory membership.
    /// </summary>
    internal sealed class DecayDiceCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly DiceInstanceId _diceId;
        private readonly SlotId _slotId;
        private readonly bool _wasWillDecay;
        private readonly bool _wasTargeted;
        private bool _wasExecuted;

        internal DecayDiceCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            DiceInstanceId diceId,
            SlotId slotId,
            bool wasWillDecay,
            bool wasTargeted)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            if (!diceId.IsValid) throw new ArgumentException("A valid dice ID is required.", nameof(diceId));
            if (!slotId.IsValid) throw new ArgumentException("A valid slot ID is required.", nameof(slotId));
            _diceId = diceId;
            _slotId = slotId;
            _wasWillDecay = wasWillDecay;
            _wasTargeted = wasTargeted;
        }

        internal DecayDiceCommandResult Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("A DecayDiceCommand can only execute once.");
            if (_battleState.CurrentPhase != BattlePhase.DecayProcess)
                throw new InvalidOperationException("Dice may only complete DECAY during DecayProcess.");

            SlotState slot = _boardState.GetSlot(_slotId);
            if (slot.Condition != SlotCondition.Unbroken)
                throw new InvalidOperationException($"DECAY requires an Unbroken target slot; {_slotId} is {slot.Condition}.");
            if (!slot.HasDice || slot.OccupantDiceId != _diceId)
                throw new InvalidOperationException($"Slot {_slotId} does not contain dice {_diceId}.");
            if (!_battleInventoryState.TryGetDice(_diceId, out DiceRuntimeState dice))
                throw new InvalidOperationException($"Battle Inventory does not track dice {_diceId}.");
            if (dice.Owner != _slotId.Side)
                throw new InvalidOperationException($"Dice {_diceId} ownership does not match slot {_slotId}.");
            if (_battleInventoryState.IsInInventory(_diceId))
                throw new InvalidOperationException($"Dice {_diceId} cannot be both on Board and in Battle Inventory.");
            if (dice.IsDecayedForCurrentGame)
                throw new InvalidOperationException($"Dice {_diceId} has already DECAYED this game.");
            if (!dice.HasCurrentFace)
                throw new InvalidOperationException($"Dice {_diceId} has no current rolled face for DECAY.");

            BattleFactContext context = _battleState.CurrentFactContext;
            int rollValue = dice.ActiveRollValue;
            _boardState.RemoveDice(_slotId);
            dice.MarkDecayedForCurrentGame();
            SlotCondition previousCondition = slot.Condition;
            _boardState.SetSlotCondition(_slotId, SlotCondition.Broken);
            _wasExecuted = true;

            return new DecayDiceCommandResult(
                new DiceDecayedFact(context, _diceId, _slotId, rollValue, _wasWillDecay, _wasTargeted),
                new SlotConditionChangedFact(context, _slotId, previousCondition, SlotCondition.Broken));
        }
    }

    internal readonly struct DecayDiceCommandResult
    {
        internal DecayDiceCommandResult(DiceDecayedFact diceDecayedFact, SlotConditionChangedFact slotConditionFact)
        {
            DiceDecayedFact = diceDecayedFact;
            SlotConditionFact = slotConditionFact;
        }

        internal DiceDecayedFact DiceDecayedFact { get; }
        internal SlotConditionChangedFact SlotConditionFact { get; }
    }
}
