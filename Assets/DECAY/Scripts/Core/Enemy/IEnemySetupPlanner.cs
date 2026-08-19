using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Chooses Enemy setup movement requests from authoritative battle state without mutating that state.
    /// EnemyController submits the resulting requests through the shared MoveDiceController authority.
    /// </summary>
    public interface IEnemySetupPlanner
    {
        IReadOnlyList<MoveDiceRequest> CreatePlan(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState);
    }
}
