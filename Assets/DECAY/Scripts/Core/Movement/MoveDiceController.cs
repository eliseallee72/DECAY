using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Single permission path for dice movement. Views, AI, tutorials, and developer tools submit
    /// MoveDiceRequest values here rather than duplicating phase/ownership/destination rules.
    /// </summary>
    public sealed class MoveDiceController
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly BattleHistory _history;
        private readonly MoveDiceGateCollection _gates;

        public MoveDiceController(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            BattleHistory history,
            IEnumerable<IMoveDiceGate> additionalGates = null)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _history = history ?? throw new ArgumentNullException(nameof(history));

            var gates = new List<IMoveDiceGate>
            {
                new MovementBattleGate(),
                new MovementControlGate(),
                new MovementSourceGate(),
                new MovementPhaseGate(),
                new MovementDestinationGate()
            };

            if (additionalGates != null)
            {
                foreach (IMoveDiceGate gate in additionalGates)
                {
                    if (gate == null)
                    {
                        throw new ArgumentException("Additional movement Gates cannot contain null entries.", nameof(additionalGates));
                    }

                    gates.Add(gate);
                }
            }

            _gates = new MoveDiceGateCollection(gates);
        }

        public MoveDiceResult RequestMove(MoveDiceRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var context = new MoveDiceGateContext(request, _battleState, _boardState, _battleInventoryState);
            MoveDiceDenialReason denial = _gates.Evaluate(context);
            if (denial != MoveDiceDenialReason.None)
            {
                return MoveDiceResult.Rejected(denial);
            }

            BattleFact fact = ExecuteApprovedMovement(context);
            _history.Record(fact);
            return MoveDiceResult.Approved(fact);
        }

        private BattleFact ExecuteApprovedMovement(MoveDiceGateContext context)
        {
            MoveDiceRequest request = context.Request;

            if (request.Target.Kind == MoveDiceTargetKind.BattleInventory)
            {
                if (!context.IsOnBoard)
                {
                    throw new InvalidOperationException("Approved inventory movement requires a board source.");
                }

                return new ReturnDiceToInventoryCommand(
                    _battleState,
                    _boardState,
                    _battleInventoryState,
                    context.SourceSlot).Execute();
            }

            SlotId destination = request.Target.BoardSlot;
            if (context.IsInInventory)
            {
                return context.DestinationSlot.HasDice
                    ? new SwapBoardWithInventoryCommand(
                        _battleState,
                        _boardState,
                        _battleInventoryState,
                        destination,
                        request.DiceId).Execute()
                    : new PlaceDiceOnBoardCommand(
                        _battleState,
                        _boardState,
                        _battleInventoryState,
                        request.DiceId,
                        destination).Execute();
            }

            if (!context.IsOnBoard)
            {
                throw new InvalidOperationException("Approved board movement requires a board or Battle Inventory source.");
            }

            return context.DestinationSlot.HasDice
                ? new SwapBoardDiceCommand(
                    _battleState,
                    _boardState,
                    _battleInventoryState,
                    context.SourceSlot,
                    destination).Execute()
                : new MoveDiceOnBoardCommand(
                    _battleState,
                    _boardState,
                    _battleInventoryState,
                    context.SourceSlot,
                    destination).Execute();
        }
    }
}
