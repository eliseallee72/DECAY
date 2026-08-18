using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Coordinates Enemy decisions at their battle-flow boundaries. It owns no board or inventory state:
    /// planners choose intended requests and the shared MoveDiceController remains authoritative over whether
    /// each movement is legal and how it mutates state.
    /// </summary>
    public sealed class EnemyController
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;
        private readonly BattleInventoryState _battleInventoryState;
        private readonly MoveDiceController _moveDiceController;
        private readonly IEnemySetupPlanner _setupPlanner;

        public EnemyController(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            MoveDiceController moveDiceController,
            IEnemySetupPlanner setupPlanner)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            _battleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            _moveDiceController = moveDiceController ?? throw new ArgumentNullException(nameof(moveDiceController));
            _setupPlanner = setupPlanner ?? throw new ArgumentNullException(nameof(setupPlanner));
        }

        public EnemySetupExecutionResult ExecuteSetup()
        {
            if (_battleState.CurrentPhase != BattlePhase.Setup)
            {
                throw new InvalidOperationException(
                    $"Enemy setup execution requires phase {BattlePhase.Setup}; current phase is {_battleState.CurrentPhase}.");
            }

            IReadOnlyList<MoveDiceRequest> plan = _setupPlanner.CreatePlan(
                _battleState,
                _boardState,
                _battleInventoryState);

            if (plan == null)
            {
                throw new InvalidOperationException("Enemy setup planner returned a null plan.");
            }

            // Validate the planner contract completely before the first authoritative movement. A malformed
            // planner must not leave a partially committed setup behind simply because an earlier request was valid.
            for (int i = 0; i < plan.Count; i++)
            {
                MoveDiceRequest request = plan[i]
                    ?? throw new InvalidOperationException($"Enemy setup planner returned a null request at index {i}.");

                if (request.ActingSide != Side.Enemy
                    || request.Target.Kind != MoveDiceTargetKind.BoardSlot
                    || request.Target.BoardSlot.Side != Side.Enemy)
                {
                    throw new InvalidOperationException(
                        $"Enemy setup planner request {i} must move an Enemy dice to an Enemy board slot.");
                }
            }

            var results = new List<MoveDiceResult>(plan.Count);
            for (int i = 0; i < plan.Count; i++)
            {
                MoveDiceRequest request = plan[i];
                MoveDiceResult result = _moveDiceController.RequestMove(request);
                if (result.IsRejected)
                {
                    throw new InvalidOperationException(
                        $"Enemy setup planner produced a movement rejected by shared authority: {result.DenialReason}.");
                }

                results.Add(result);
            }

            return new EnemySetupExecutionResult(results.AsReadOnly());
        }
    }
}
