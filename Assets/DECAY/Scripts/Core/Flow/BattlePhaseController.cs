using System;

namespace Decay
{
    /// <summary>
    /// Internal phase-transition authority used by BattleController. It validates the explicit phase graph,
    /// progression constraints, and records the completed PhaseChangedFact. Gameplay callers must request
    /// phase/process progression through BattleController so blocking process boundaries cannot be bypassed.
    /// </summary>
    public sealed class BattlePhaseController
    {
        private readonly BattleState _battleState;
        private readonly BattlePhaseTransitionValidator _transitionValidator;
        private readonly BattleHistory _history;
        private readonly GameEndCondition _gameEndCondition;

        public BattlePhaseController(
            BattleState battleState,
            BoardState boardState,
            BattlePhaseTransitionValidator transitionValidator,
            BattleHistory history)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _transitionValidator = transitionValidator ?? throw new ArgumentNullException(nameof(transitionValidator));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _gameEndCondition = new GameEndCondition(_battleState, boardState ?? throw new ArgumentNullException(nameof(boardState)));
        }

        internal PhaseChangeResult Handle(PhaseChangeRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (_battleState.IsBattleComplete)
            {
                return PhaseChangeResult.Rejected(PhaseChangeDenialReason.BattleAlreadyComplete);
            }

            BattlePhase currentPhase = _battleState.CurrentPhase;
            BattlePhase requestedPhase = request.RequestedPhase;

            if (!_transitionValidator.IsTransitionAllowed(currentPhase, requestedPhase))
            {
                return PhaseChangeResult.Rejected(PhaseChangeDenialReason.TransitionNotAllowed);
            }

            PhaseChangeDenialReason progressionDenial = GetProgressionDenial(currentPhase, requestedPhase);
            if (progressionDenial != PhaseChangeDenialReason.None)
            {
                return PhaseChangeResult.Rejected(progressionDenial);
            }

            var command = new AdvancePhaseCommand(_battleState, requestedPhase);
            PhaseChangedFact fact = command.Execute();
            _history.Record(fact);
            return PhaseChangeResult.Approved(fact);
        }

        private PhaseChangeDenialReason GetProgressionDenial(BattlePhase currentPhase, BattlePhase requestedPhase)
        {
            if (currentPhase == BattlePhase.RoundEnd)
            {
                if (requestedPhase == BattlePhase.Setup && _gameEndCondition.IsGameEndRequired)
                {
                    return _gameEndCondition.IsRoundLimitReached
                        ? PhaseChangeDenialReason.RoundLimitRequiresGameEnd
                        : PhaseChangeDenialReason.BoardBreakRequiresGameEnd;
                }

                if (requestedPhase == BattlePhase.GameEnd && !_gameEndCondition.IsGameEndRequired)
                {
                    return PhaseChangeDenialReason.GameEndConditionNotMet;
                }
            }

            if (currentPhase == BattlePhase.GameEnd && requestedPhase == BattlePhase.Setup)
            {
                return _battleState.CurrentGameNumber >= _battleState.GamesPerBattle
                    ? PhaseChangeDenialReason.FinalGameRequiresBattleEnd
                    : PhaseChangeDenialReason.None;
            }

            if (currentPhase == BattlePhase.GameEnd && requestedPhase == BattlePhase.BattleEnd)
            {
                return _battleState.CurrentGameNumber < _battleState.GamesPerBattle
                    ? PhaseChangeDenialReason.MoreGamesRemain
                    : PhaseChangeDenialReason.None;
            }

            return PhaseChangeDenialReason.None;
        }
    }
}
