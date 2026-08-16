using System;

namespace Decay
{
    /// <summary>
    /// Approved authoritative handoff from Enemy Setup to Player Setup.
    /// </summary>
    public sealed class CompleteEnemySetupCommand
    {
        private readonly BattleState _battleState;
        private bool _wasExecuted;

        internal CompleteEnemySetupCommand(BattleState battleState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
        }

        internal SetupTurnChangedFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("A CompleteEnemySetupCommand can only execute once.");
            }

            BattleFactContext context = _battleState.CurrentFactContext;
            BattleSetupTurn previousTurn = _battleState.CurrentSetupTurn;
            _battleState.ApplyEnemySetupCompleted();
            _wasExecuted = true;

            return new SetupTurnChangedFact(context, previousTurn, _battleState.CurrentSetupTurn);
        }
    }
}
