using System;

namespace Decay
{
    public sealed class SwapBoardDiceCommand
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly SlotId _firstSlot;
        private readonly SlotId _secondSlot;
        private bool _wasExecuted;

        internal SwapBoardDiceCommand(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            SlotId firstSlot,
            SlotId secondSlot)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _firstSlot = firstSlot;
            _secondSlot = secondSlot;
        }

        internal BoardDiceSwappedFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A SwapBoardDiceCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            SlotState first = _boardState.GetSlot(_firstSlot);
            SlotState second = _boardState.GetSlot(_secondSlot);

            if (!first.HasDice || !second.HasDice)
            {
                throw new InvalidOperationException("Both slots must contain dice for a board swap.");
            }

            DiceInstanceId firstDiceId = first.OccupantDiceId;
            DiceInstanceId secondDiceId = second.OccupantDiceId;
            DiceRuntimeState firstDice = _battleInventoryState.GetDice(firstDiceId);
            DiceRuntimeState secondDice = _battleInventoryState.GetDice(secondDiceId);

            if (firstDice.Owner != secondDice.Owner
                || firstDice.Owner != _firstSlot.Side
                || firstDice.Owner != _secondSlot.Side)
            {
                throw new InvalidOperationException("Board swap cannot cross between Enemy and Player sides.");
            }

            if (_battleInventoryState.IsInInventory(firstDiceId) || _battleInventoryState.IsInInventory(secondDiceId))
            {
                throw new InvalidOperationException("Board dice cannot simultaneously be members of Battle Inventory.");
            }

            _boardState.SwapDice(_firstSlot, _secondSlot);
            _wasExecuted = true;

            return new BoardDiceSwappedFact(
                context,
                firstDice.Owner,
                firstDiceId,
                _firstSlot,
                _secondSlot,
                secondDiceId,
                _secondSlot,
                _firstSlot);
        }
    }
}
