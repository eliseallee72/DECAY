using System;

namespace Decay
{
    public sealed class MoveDiceOnBoardCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly SlotId _sourceSlot;
        private readonly SlotId _destinationSlot;
        private bool _wasExecuted;

        internal MoveDiceOnBoardCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            SlotId sourceSlot,
            SlotId destinationSlot)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _sourceSlot = sourceSlot;
            _destinationSlot = destinationSlot;
        }

        internal DiceMovedOnBoardFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A MoveDiceOnBoardCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            SlotState source = _boardState.GetSlot(_sourceSlot);
            if (!source.HasDice)
            {
                throw new InvalidOperationException($"Source slot {_sourceSlot} is empty.");
            }

            DiceInstanceId diceId = source.OccupantDiceId;
            DiceRuntimeState diceState = _battleInventoryState.GetDice(diceId);

            if (diceState.Owner != _sourceSlot.Side || diceState.Owner != _destinationSlot.Side)
            {
                throw new InvalidOperationException("Board movement cannot cross between Enemy and Player sides.");
            }

            if (_battleInventoryState.IsInInventory(diceId))
            {
                throw new InvalidOperationException("A dice cannot be on the board and in Battle Inventory at the same time.");
            }

            _boardState.MoveDice(_sourceSlot, _destinationSlot);
            _wasExecuted = true;
            return new DiceMovedOnBoardFact(context, diceId, diceState.Owner, _sourceSlot, _destinationSlot);
        }
    }
}
