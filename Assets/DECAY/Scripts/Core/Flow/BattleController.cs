using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns bounded round-flow orchestration across already-authoritative systems.
    ///
    /// Current first-pass sequence:
    /// EnemySetup -> PlayerSetup -> Rolling -> EnemyReposition -> PlayerReposition -> DecayProcess.
    ///
    /// This controller does not choose enemy moves, validate dice movement, resolve random faces, or
    /// calculate DECAY/SCORE results. Those responsibilities remain with their dedicated authorities.
    /// </summary>
    public sealed class BattleController
    {
        private readonly BattleState _battleState;
        private readonly BattlePhaseController _phaseController;
        private readonly BattleHistory _history;
        private readonly RollExecutor _rollExecutor;
        private bool _isRollResolvedAwaitingCompletion;

        public BattleController(
            BattleState battleState,
            BattlePhaseController phaseController,
            BattleHistory history,
            RollExecutor rollExecutor)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _phaseController = phaseController ?? throw new ArgumentNullException(nameof(phaseController));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _rollExecutor = rollExecutor ?? throw new ArgumentNullException(nameof(rollExecutor));
        }

        /// <summary>
        /// Completes the enemy-controlled setup phase and hands setup interaction authority to PlayerSetup.
        /// Future EnemyController setup planning should submit its MoveDiceRequests first, then call this
        /// boundary once its approved setup plan is finished.
        /// </summary>
        public BattleFlowResult CompleteEnemySetup()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.EnemySetup);
            if (commonRejection != null)
            {
                return commonRejection;
            }

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.PlayerSetup);
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Player setup completion request. Enters Rolling and invokes the logical Roll exactly once.
        /// The controller remains in Rolling until CompleteRoll is called so a future blocking roll
        /// presentation can finish without rule code guessing an animation duration.
        /// </summary>
        public BattleFlowResult RequestRoll()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.PlayerSetup);
            if (commonRejection != null)
            {
                return commonRejection;
            }

            int firstFactIndex = _history.Count;
            _isRollResolvedAwaitingCompletion = false;

            RequireApprovedTransition(BattlePhase.Rolling);
            _rollExecutor.ExecuteRoll();
            _isRollResolvedAwaitingCompletion = true;

            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes the Roll process boundary after its authoritative results are resolved and any
        /// required blocking presentation has finished. Until that presentation layer exists, the
        /// composition root may call this immediately after handling RequestRoll's approved result.
        /// </summary>
        public BattleFlowResult CompleteRoll()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.Rolling);
            if (commonRejection != null)
            {
                return commonRejection;
            }

            if (!_isRollResolvedAwaitingCompletion)
            {
                return BattleFlowResult.Rejected(BattleFlowDenialReason.RollNotResolved);
            }

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.EnemyReposition);
            _isRollResolvedAwaitingCompletion = false;
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Completes the future EnemyController reposition plan and hands board-only reposition
        /// authority to the Player.
        /// </summary>
        public BattleFlowResult CompleteEnemyReposition()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.EnemyReposition);
            if (commonRejection != null)
            {
                return commonRejection;
            }

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.PlayerReposition);
            return ApprovedFactsSince(firstFactIndex);
        }

        /// <summary>
        /// Player Reposition completion request from the future Hourglass input route. This first pass
        /// only enters DecayProcess; it does not invent placeholder DECAY results before that resolver
        /// and executor are implemented.
        /// </summary>
        public BattleFlowResult RequestDecay()
        {
            BattleFlowResult commonRejection = RejectIfBattleCompleteOrWrongPhase(BattlePhase.PlayerReposition);
            if (commonRejection != null)
            {
                return commonRejection;
            }

            int firstFactIndex = _history.Count;
            RequireApprovedTransition(BattlePhase.DecayProcess);
            return ApprovedFactsSince(firstFactIndex);
        }

        private BattleFlowResult RejectIfBattleCompleteOrWrongPhase(BattlePhase requiredPhase)
        {
            if (_battleState.IsBattleComplete)
            {
                return BattleFlowResult.Rejected(BattleFlowDenialReason.BattleAlreadyComplete);
            }

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
            {
                throw new ArgumentOutOfRangeException(nameof(firstFactIndex));
            }

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++)
            {
                facts.Add(_history.Facts[i]);
            }

            return BattleFlowResult.Approved(facts.AsReadOnly());
        }
    }
}
