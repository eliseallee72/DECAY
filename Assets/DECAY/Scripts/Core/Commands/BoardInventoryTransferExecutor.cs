using System;

namespace Decay
{
    /// <summary>
    /// Coordinates membership changes that cross the BoardState/BattleInventoryState boundary.
    /// It does not own dice location; BoardState and BattleInventoryState remain authoritative.
    /// All failure-prone invariants are checked before the first mutation so callers do not
    /// duplicate transfer order or rollback behavior.
    /// </summary>
    internal sealed class BoardInventoryTransferExecutor
    {
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;

        internal BoardInventoryTransferExecutor(BoardState boardState, BattleInventoryState battleInventoryState)
        {
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
        }

        internal void PlaceFromInventory(DiceInstanceId diceId, SlotId destinationSlot)
        {
            DiceRuntimeState diceState = _battleInventoryState.RequireCanRemoveFromInventory(diceId);
            SlotState destination = _boardState.GetSlot(destinationSlot);

            if (diceState.Owner != destinationSlot.Side)
            {
                throw new InvalidOperationException("Dice can only be placed into slots belonging to their own side.");
            }

            if (diceState.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException("A DECAYED dice cannot be placed on the board again during the current game.");
            }

            if (_boardState.IsDiceOnBoard(diceId))
            {
                throw new InvalidOperationException($"Dice {diceId} is already on the board.");
            }

            if (destination.Condition != SlotCondition.Unbroken || destination.HasDice)
            {
                throw new InvalidOperationException($"Destination slot {destinationSlot} must be empty and Unbroken.");
            }

            _battleInventoryState.RemoveFromInventory(diceId);
            _boardState.PlaceDice(destinationSlot, diceId);
        }

        internal DiceInstanceId ReturnFromBoardToInventory(SlotId sourceSlot)
        {
            SlotState source = _boardState.GetSlot(sourceSlot);
            if (!source.HasDice)
            {
                throw new InvalidOperationException($"Source slot {sourceSlot} is empty.");
            }

            DiceInstanceId diceId = source.OccupantDiceId;
            DiceRuntimeState diceState = _battleInventoryState.RequireCanReturnToInventory(diceId);

            if (diceState.Owner != sourceSlot.Side)
            {
                throw new InvalidOperationException("Board occupancy and Battle Inventory ownership disagree.");
            }

            _boardState.RemoveDice(sourceSlot);
            _battleInventoryState.ReturnToInventory(diceId);
            return diceId;
        }

        internal DiceInstanceId SwapBoardWithInventory(SlotId boardSlot, DiceInstanceId inventoryDiceId)
        {
            SlotState slot = _boardState.GetSlot(boardSlot);
            if (!slot.HasDice)
            {
                throw new InvalidOperationException($"Board slot {boardSlot} must contain dice for an inventory swap.");
            }

            if (slot.Condition != SlotCondition.Unbroken)
            {
                throw new InvalidOperationException($"Board slot {boardSlot} must be Unbroken for an inventory swap.");
            }

            DiceInstanceId boardDiceId = slot.OccupantDiceId;
            DiceRuntimeState boardDice = _battleInventoryState.RequireCanReturnToInventory(boardDiceId);
            DiceRuntimeState inventoryDice = _battleInventoryState.RequireCanRemoveFromInventory(inventoryDiceId);

            if (boardDice.Owner != boardSlot.Side || inventoryDice.Owner != boardSlot.Side)
            {
                throw new InvalidOperationException("Board/inventory swap cannot cross between Enemy and Player sides.");
            }

            if (inventoryDice.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException("A DECAYED dice cannot return to the board during the current game.");
            }

            if (_boardState.IsDiceOnBoard(inventoryDiceId))
            {
                throw new InvalidOperationException("The incoming Battle Inventory dice cannot already be on the board.");
            }

            _battleInventoryState.RemoveFromInventory(inventoryDiceId);
            _boardState.RemoveDice(boardSlot);
            _boardState.PlaceDice(boardSlot, inventoryDiceId);
            _battleInventoryState.ReturnToInventory(boardDiceId);
            return boardDiceId;
        }
    }
}
