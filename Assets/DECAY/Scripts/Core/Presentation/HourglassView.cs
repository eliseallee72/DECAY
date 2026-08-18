using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass presentation/input surface. It collects pointer input and submits the appropriate battle-flow
    /// requests to the composition root; it never changes phases or mutates gameplay state directly.
    ///
    /// Until authored blocking presentation is restored, clicks use the immediate fallback path:
    /// Setup -> Roll -> EnemyReposition completion -> PlayerReposition, then
    /// PlayerReposition -> Decay -> Score -> RoundEnd -> next Setup/GameEnd.
    /// The existing explicit completion methods remain the seams that later animations will wait on.
    /// </summary>
    public sealed class HourglassView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Collider _interactionCollider;
        [SerializeField] private BattleCompositionRoot _compositionRoot;

        public bool TryValidate(out string error)
        {
            if (_camera == null)
            {
                error = $"{name}: HourglassView requires a Camera reference.";
                return false;
            }

            if (_interactionCollider == null)
            {
                error = $"{name}: HourglassView requires an interaction Collider.";
                return false;
            }

            if (_compositionRoot == null)
            {
                error = $"{name}: HourglassView requires a BattleCompositionRoot reference.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public BattleFlowResult RequestRoll()
        {
            if (!TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            return _compositionRoot.RequestRoll();
        }

        public BattleFlowResult RequestDecay()
        {
            if (!TryValidate(out string error))
            {
                throw new InvalidOperationException(error);
            }

            return _compositionRoot.RequestDecay();
        }

        /// <summary>
        /// Completion hook for authored Roll presentation. Rules have already resolved when Roll starts;
        /// this reports that blocking presentation is finished so BattleController may enter EnemyReposition.
        /// </summary>
        public void NotifyRollPresentationComplete()
        {
            if (_compositionRoot == null || !_compositionRoot.IsInitialized)
            {
                return;
            }

            _compositionRoot.CompleteRoll();
        }

        internal void ConfigureForTests(
            Camera camera,
            Collider interactionCollider,
            BattleCompositionRoot compositionRoot)
        {
            _camera = camera;
            _interactionCollider = interactionCollider;
            _compositionRoot = compositionRoot;
        }

        private void Update()
        {
            if (_compositionRoot == null
                || !_compositionRoot.IsInitialized
                || _camera == null
                || _interactionCollider == null
                || Mouse.current == null
                || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            BattlePhase phase = _compositionRoot.Runtime.BattleState.CurrentPhase;
            if (phase != BattlePhase.Setup && phase != BattlePhase.PlayerReposition)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!_interactionCollider.Raycast(ray, out _, _camera.farClipPlane))
            {
                return;
            }

            if (phase == BattlePhase.Setup)
            {
                AdvanceRollImmediatelyToPlayerReposition();
                return;
            }

            AdvanceDecayImmediatelyToNextPlayableState();
        }

        private void AdvanceRollImmediatelyToPlayerReposition()
        {
            BattleFlowResult roll = RequestRoll();
            if (!roll.IsApproved)
            {
                return;
            }

            BattleFlowResult rollCompletion = _compositionRoot.CompleteRoll();
            if (!rollCompletion.IsApproved)
            {
                return;
            }

            // Enemy reposition strategy/presentation is intentionally still deferred. For this bare playable
            // pass, completing the empty boundary preserves the authoritative phase order and gives control
            // to PlayerReposition without inventing Enemy board mutations here.
            _compositionRoot.CompleteEnemyReposition();
        }

        private void AdvanceDecayImmediatelyToNextPlayableState()
        {
            BattleFlowResult decay = RequestDecay();
            if (!decay.IsApproved)
            {
                return;
            }

            BattleFlowResult decayCompletion = _compositionRoot.CompleteDecay();
            if (!decayCompletion.IsApproved)
            {
                return;
            }

            BattleFlowResult scoreCompletion = _compositionRoot.CompleteScore();
            if (!scoreCompletion.IsApproved)
            {
                return;
            }

            BattleFlowResult roundCompletion = _compositionRoot.CompleteRoundEnd();
            if (!roundCompletion.IsApproved)
            {
                return;
            }

            if (_compositionRoot.Runtime.BattleState.CurrentPhase == BattlePhase.GameEnd)
            {
                _compositionRoot.CompleteGameEnd();
            }
        }
    }
}
