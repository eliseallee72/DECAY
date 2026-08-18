using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Completed Enemy setup movements in the same deterministic order they were submitted to movement authority.
    /// Movement Facts remain the authoritative history records; this receipt is for orchestration/presentation.
    /// </summary>
    public sealed class EnemySetupExecutionResult
    {
        internal EnemySetupExecutionResult(IReadOnlyList<MoveDiceResult> movements)
        {
            Movements = movements ?? throw new ArgumentNullException(nameof(movements));
        }

        public IReadOnlyList<MoveDiceResult> Movements { get; }
        public int MovementCount => Movements.Count;
    }
}
