using System;

namespace Decay
{
    public sealed class AdvancePhaseCommand
    {
        private readonly BattleState _battleState;
        private readonly BattlePhase _requestedPhase;
        private bool _wasExecuted;

        internal AdvancePhaseCommand(BattleState battleState, BattlePhase requestedPhase)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _requestedPhase = requestedPhase;
        }

        internal PhaseChangedFact Execute()
        {
            if (_wasExecuted)
            {
                throw new InvalidOperationException("An AdvancePhaseCommand can only execute once.");
            }

            BattleFactContext previousContext = _battleState.CurrentFactContext;
            _battleState.ApplyApprovedPhaseTransition(_requestedPhase);
            BattleFactContext currentContext = _battleState.CurrentFactContext;
            _wasExecuted = true;

            return new PhaseChangedFact(previousContext, currentContext);
        }
    }
}
