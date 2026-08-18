using System;

namespace Decay
{
    public sealed class PlaceDiceOnBoardCommand
    {
        private readonly BattleState _battleState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BoardInventoryTransferExecutor _transferExecutor;
        private readonly DiceInstanceId _diceId;
        private readonly SlotId _destinationSlot;
        private bool _wasExecuted;

        internal PlaceDiceOnBoardCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            DiceInstanceId diceId,
            SlotId destinationSlot)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _transferExecutor = new BoardInventoryTransferExecutor(
                boardState ?? throw new ArgumentNullException(nameof(boardState)),
                _battleInventoryState);
            _diceId = diceId;
            _destinationSlot = destinationSlot;
        }

        internal DicePlacedOnBoardFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A PlaceDiceOnBoardCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            DiceRuntimeState diceState = _battleInventoryState.GetDice(_diceId);
            _transferExecutor.PlaceFromInventory(_diceId, _destinationSlot);

            _wasExecuted = true;
            return new DicePlacedOnBoardFact(context, _diceId, diceState.Owner, _destinationSlot);
        }
    }
}
