using System;

namespace Decay
{
    public sealed class ReturnDiceToInventoryCommand
    {
        private readonly BattleState _battleState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BoardInventoryTransferExecutor _transferExecutor;
        private readonly SlotId _sourceSlot;
        private bool _wasExecuted;

        internal ReturnDiceToInventoryCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            SlotId sourceSlot)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _transferExecutor = new BoardInventoryTransferExecutor(
                boardState ?? throw new ArgumentNullException(nameof(boardState)),
                _battleInventoryState);
            _sourceSlot = sourceSlot;
        }

        internal DiceReturnedToInventoryFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A ReturnDiceToInventoryCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            DiceInstanceId diceId = _transferExecutor.ReturnFromBoardToInventory(_sourceSlot);
            DiceRuntimeState diceState = _battleInventoryState.GetDice(diceId);

            _wasExecuted = true;
            return new DiceReturnedToInventoryFact(context, diceId, diceState.Owner, _sourceSlot);
        }
    }
}
