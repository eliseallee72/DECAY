using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns bounded battle-flow orchestration across already-authoritative systems.
    /// Rule calculations remain with their dedicated resolvers/executors; this controller owns only the explicit
    /// process boundaries and lifecycle transitions needed to run the complete two-game battle loop.
    /// </summary>
    public sealed class BattleController
    {
        private readonly BattleState _battleState;
        private readonly BattlePhaseController _phaseController;
        private readonly BattleHistory _history;
        private readonly ScoreState _scoreState;
        private readonly RollExecutor _rollExecutor;
        private readonly DecayExecutor _decayExecutor;
        private readonly ScoreExecutor _scoreExecutor;
        private readonly RoundEndExecutor _roundEndExecutor;
        private readonly GameEndExecutor _gameEndExecutor;
        private RollExecutionResult _activeRollExecution;
        private DecayExecutionResult _activeDecayExecution;
        private ScoreExecutionResult _activeScoreExecution;
        private RoundEndExecutionResult _activeRoundEndExecution;
        private GameEndExecutionResult _activeGameEndExecution;

        public BattleController(
            BattleState battleState,
            BattlePhaseController phaseController,
            BattleHistory history,
            ScoreState scoreState,
            RollExecutor rollExecutor,
            DecayExecutor decayExecutor,
            ScoreExecutor scoreExecutor,
            RoundEndExecutor roundEndExecutor,
            GameEndExecutor gameEndExecutor)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _phaseController = phaseController ?? throw new ArgumentNullException(nameof(phaseController));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _rollExecutor = rollExecutor ?? throw new ArgumentNullException(nameof(rollExecutor));
            _decayExecutor = decayExecutor ?? throw new ArgumentNullException(nameof(decayExecutor));
            _scoreExecutor = scoreExecutor ?? throw new ArgumentNullException(nameof(scoreExecutor));
            _roundEndExecutor = roundEndExecutor ?? throw new ArgumentNullException(nameof(roundEndExecutor));
            _gameEndExecutor = gameEndExecutor ?? throw new ArgumentNullException(nameof(gameEndExecutor));
        }

        public BattleFlowResult RequestRoll()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.Setup);
            if (commonRejection != null) return commonRejection;

            int firstFactIndex = _history.Count;
            _activeRollExecution = null;
            RequireApprovedTransition(BattlePhase.Rolling);
            _activeRollExecution = _rollExecutor.ExecuteRoll();
            return ApprovedFactsSince(firstFactIndex);
        }

        public BattleFlowResult CompleteRoll()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.Rolling);
            if (commonRejection != null) return commonRejection;

            BattleFlowDenialReason completionDenial = _rollExecutor.EvaluateCompletion(_activeRollExecution);
            if (completionDenial != BattleFlowDenialReason.None) return BattleFlowResult.Rejected(completionDenial);

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.EnemyReposition);
            _activeRollExecution = null;
            return ApprovedFactsSince(firstFactIndex);
        }

        public BattleFlowResult CompleteEnemyReposition()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.EnemyReposition);
            if (commonRejection != null) return commonRejection;

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.PlayerReposition);
            return ApprovedFactsSince(firstFactIndex);
        }

        public BattleFlowResult RequestDecay()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.PlayerReposition);
            if (commonRejection != null) return commonRejection;

            int firstFactIndex = _history.Count;
            _activeDecayExecution = null;
            RequireApprovedTransition(BattlePhase.DecayProcess);
            _activeDecayExecution = _decayExecutor.ExecuteDecay();
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes DECAY and starts the logical SCORE process exactly once. The controller remains in ScoreProcess
        /// until CompleteScore so future score/abacus presentation can play from the committed ScoreAppliedFacts.
        /// </summary>
        public BattleFlowResult CompleteDecay()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.DecayProcess);
            if (commonRejection != null) return commonRejection;

            BattleFlowDenialReason completionDenial = _decayExecutor.EvaluateCompletion(_activeDecayExecution);
            if (completionDenial != BattleFlowDenialReason.None) return BattleFlowResult.Rejected(completionDenial);

            int firstFactIndex = _history.Count;
            _activeScoreExecution = null;
            RequireApprovedTransition(BattlePhase.ScoreProcess);
            _activeDecayExecution = null;
            _activeScoreExecution = _scoreExecutor.ExecuteScore();
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes SCORE after any blocking presentation, enters RoundEnd, and performs authoritative round cleanup.
        /// RoundEnd remains explicit until CompleteRoundEnd so presentation never has to guess when cleanup finished.
        /// </summary>
        public BattleFlowResult CompleteScore()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.ScoreProcess);
            if (commonRejection != null) return commonRejection;

            BattleFlowDenialReason completionDenial = _scoreExecutor.EvaluateCompletion(_activeScoreExecution);
            if (completionDenial != BattleFlowDenialReason.None) return BattleFlowResult.Rejected(completionDenial);

            int firstFactIndex = _history.Count;
            _activeRoundEndExecution = null;
            RequireApprovedTransition(BattlePhase.RoundEnd);
            _activeScoreExecution = null;
            _activeRoundEndExecution = _roundEndExecutor.ExecuteRoundEnd();
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes round cleanup. A continuing game explicitly starts the next Setup; otherwise GameEnd is
        /// entered and the game score/reset work is executed once behind its own completion boundary.
        /// </summary>
        public BattleFlowResult CompleteRoundEnd()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.RoundEnd);
            if (commonRejection != null) return commonRejection;

            BattleFlowDenialReason completionDenial = _roundEndExecutor.EvaluateCompletion(_activeRoundEndExecution);
            if (completionDenial != BattleFlowDenialReason.None) return BattleFlowResult.Rejected(completionDenial);

            int firstFactIndex = _history.Count;
            bool gameEndRequired = _activeRoundEndExecution.GameEndRequired;
            _activeRoundEndExecution = null;

            if (!gameEndRequired)
            {
                RequireApprovedTransition(BattlePhase.Setup);
                return ApprovedFactsSince(firstFactIndex);
            }

            _activeGameEndExecution = null;
            RequireApprovedTransition(BattlePhase.GameEnd);
            _activeGameEndExecution = _gameEndExecutor.ExecuteGameEnd();
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes GameEnd. If another configured game remains, BattleState advances to its Setup. After the
        /// final game, the battle enters BattleEnd and records the derived winner/draw from authoritative ScoreState.
        /// </summary>
        public BattleFlowResult CompleteGameEnd()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.GameEnd);
            if (commonRejection != null) return commonRejection;

            BattleFlowDenialReason completionDenial = _gameEndExecutor.EvaluateCompletion(_activeGameEndExecution);
            if (completionDenial != BattleFlowDenialReason.None) return BattleFlowResult.Rejected(completionDenial);

            int firstFactIndex = _history.Count;
            _activeGameEndExecution = null;

            if (_battleState.CurrentGameNumber < _battleState.GamesPerBattle)
            {
                RequireApprovedTransition(BattlePhase.Setup);
                return ApprovedFactsSince(firstFactIndex);
            }

            RequireApprovedTransition(BattlePhase.BattleEnd);
            _history.Record(new BattleEndedFact(
                _battleState.CurrentFactContext,
                _scoreState.GetBattleScore(Side.Enemy),
                _scoreState.GetBattleScore(Side.Player),
                _scoreState.GetCurrentBattleOutcome()));
            return ApprovedFactsSince(firstFactIndex);
        }

        private BattleFlowResult RejectIfBattleCompleteOrWrongPhase(BattlePhase requiredPhase)
        {
            if (_battleState.IsBattleComplete)
                return BattleFlowResult.Rejected(BattleFlowDenialReason.BattleAlreadyComplete);

            return _battleState.CurrentPhase == requiredPhase
                ? null
                : BattleFlowResult.Rejected(BattleFlowDenialReason.WrongPhase);
        }

        private void RequireApprovedTransition(BattlePhase requestedPhase)
        {
            PhaseChangeResult transition = _phaseController.Handle(new PhaseChangeRequest(requestedPhase));
            if (!transition.IsApproved)
            {
                throw new InvalidOperationException(
                    $"BattleController expected transition to {requestedPhase} to be approved, "
                    + $"but BattlePhaseController rejected it with {transition.DenialReason}.");
            }
        }

        private BattleFlowResult ApprovedFactsSince(int firstFactIndex)
        {
            if (firstFactIndex < 0 || firstFactIndex > _history.Count)
                throw new ArgumentOutOfRangeException(nameof(firstFactIndex));

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++) facts.Add(_history.Facts[i]);
            return BattleFlowResult.Approved(facts.AsReadOnly());
        }
    }
}
