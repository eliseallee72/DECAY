using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass presentation/input surface. It collects pointer input and submits the appropriate battle-flow
    /// request to the composition root; it never advances phases or mutates gameplay state directly.
    /// Roll presentation may later call NotifyRollPresentationComplete from an Animation Event or presenter.
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
                || !Mouse.current.leftButton.wasPressedThisFrame
                || _compositionRoot.Runtime.BattleState.CurrentPhase != BattlePhase.Setup)
            {
                return;
            }

            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (_interactionCollider.Raycast(ray, out _, _camera.farClipPlane))
            {
                RequestRoll();
            }
        }
    }
}
