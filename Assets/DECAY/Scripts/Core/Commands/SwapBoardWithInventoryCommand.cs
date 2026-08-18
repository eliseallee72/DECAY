using System;

namespace Decay
{
    /// <summary>
    /// Atomic membership swap used by Setup flows: one board dice returns to Battle Inventory
    /// while one Battle Inventory dice takes its slot. Phase permission belongs to Gates/controllers.
    /// </summary>
    public sealed class SwapBoardWithInventoryCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BoardInventoryTransferExecutor _transferExecutor;
        private readonly SlotId _boardSlot;
        private readonly DiceInstanceId _inventoryDiceId;
        private bool _wasExecuted;

        internal SwapBoardWithInventoryCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            SlotId boardSlot,
            DiceInstanceId inventoryDiceId)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _transferExecutor = new BoardInventoryTransferExecutor(_boardState, _battleInventoryState);
            _boardSlot = boardSlot;
            _inventoryDiceId = inventoryDiceId;
        }

        internal BoardInventoryDiceSwappedFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A SwapBoardWithInventoryCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            SlotState slot = _boardState.GetSlot(_boardSlot);
            if (!slot.HasDice)
            {
                throw new InvalidOperationException($"Board slot {_boardSlot} must contain dice for an inventory swap.");
            }

            DiceInstanceId boardDiceId = slot.OccupantDiceId;
            DiceRuntimeState boardDice = _battleInventoryState.GetDice(boardDiceId);

            _transferExecutor.SwapBoardWithInventory(_boardSlot, _inventoryDiceId);

            _wasExecuted = true;
            return new BoardInventoryDiceSwappedFact(
                context,
                boardDice.Owner,
                _boardSlot,
                boardDiceId,
                _inventoryDiceId);
        }
    }
}
