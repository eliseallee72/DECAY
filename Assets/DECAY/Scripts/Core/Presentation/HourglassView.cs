using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass input/presentation surface. Pointer input only raises an interaction request; authoritative battle flow
    /// decides what that request means. Authored and optional procedural presentation then reflect that authoritative result.
    /// </summary>
    public sealed class HourglassView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Event-Driven Pointer Input")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Collider _interactionCollider;
        [Tooltip("Input System binding used to request interaction. Stored as editor data rather than polled in Update.")]
        [SerializeField] private string _pressBindingPath = "<Mouse>/leftButton";

        [Header("Persistent Authoritative Presentation")]
        [Tooltip("Animator int representing the authoritative BattlePhase. Animation state never establishes the phase.")]
        [SerializeField] private AnimatorIntPresentationBinding _phasePresentation = new AnimatorIntPresentationBinding();
        [Tooltip("Optional trigger used after cancellation/reconciliation to return to the persistent phase state.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();
        [Tooltip("Presentation-only hover state. This does not imply the hourglass interaction will be accepted.")]
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();

        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _rollToRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _decayPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetToSetupPresentation = new AnimatorTriggerPresentationBinding();

        [Header("Optional Procedural Layers")]
        [SerializeField] private ProceduralTransformPresentationBinding _rollToRepositionMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _decayMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _resetToSetupMotion = new ProceduralTransformPresentationBinding();

        private InputAction _pressAction;
        private Action _interactionRequested;
        private HybridOneShotPresentationRun _rollRun;
        private HybridOneShotPresentationRun _decayRun;
        private HybridOneShotPresentationRun _resetRun;

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

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
            if (string.IsNullOrWhiteSpace(_pressBindingPath))
            {
                error = $"{name}: HourglassView requires an Input System press binding path.";
                return false;
            }
            if (!_phasePresentation.TryValidate($"{name} Phase", out error)
                || !_reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                || !_hoverPresentation.TryValidate($"{name} Hover", out error)
                || !_rollToRepositionPresentation.TryValidate($"{name} Roll Presentation", out error)
                || !_decayPresentation.TryValidate($"{name} Decay Presentation", out error)
                || !_resetToSetupPresentation.TryValidate($"{name} Reset Presentation", out error)
                || !_rollToRepositionMotion.TryValidate($"{name} Roll Motion", out error)
                || !_decayMotion.TryValidate($"{name} Decay Motion", out error)
                || !_resetToSetupMotion.TryValidate($"{name} Reset Motion", out error))
                return false;

            error = string.Empty;
            return true;
        }

        internal void BindInteraction(Action interactionRequested)
        {
            _interactionRequested = interactionRequested ?? throw new ArgumentNullException(nameof(interactionRequested));
        }

        internal void UnbindInteraction(Action interactionRequested)
        {
            if (_interactionRequested == interactionRequested)
                _interactionRequested = null;
        }

        internal void ReconcilePhase(BattlePhase phase, bool invokeRecoveryHook = false)
        {
            _phasePresentation.SetValue((int)phase);
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void PlayRollPresentation(Action onCompleted) =>
            StartHybrid(_rollToRepositionPresentation, _rollToRepositionMotion, ref _rollRun, onCompleted);

        internal void PlayDecayPresentation(Action onCompleted) =>
            StartHybrid(_decayPresentation, _decayMotion, ref _decayRun, onCompleted);

        internal void PlayResetPresentation(Action onCompleted) =>
            StartHybrid(_resetToSetupPresentation, _resetToSetupMotion, ref _resetRun, onCompleted);

        /// <summary>
        /// Public editor/test endpoint for any alternate input surface. It only requests interaction; it never advances flow.
        /// </summary>
        public void NotifyInteractionRequested() => _interactionRequested?.Invoke();

        public void NotifyRollPresentationComplete() => _rollRun?.NotifyAuthoredComplete();
        public void NotifyDecayPresentationComplete() => _decayRun?.NotifyAuthoredComplete();
        public void NotifyResetPresentationComplete() => _resetRun?.NotifyAuthoredComplete();

        internal void CancelRollPresentation() => CancelRun(ref _rollRun);

        internal void CancelAllPresentation()
        {
            CancelRun(ref _rollRun);
            CancelRun(ref _decayRun);
            CancelRun(ref _resetRun);
            _hoverPresentation.SetActive(false);
        }

        internal void ConfigureForTests(Camera camera, Collider interactionCollider)
        {
            _camera = camera;
            _interactionCollider = interactionCollider;
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) => _hoverPresentation.SetActive(isHovered);

        // The hourglass already has a functional click request. Decorative press feedback is intentionally not duplicated here.
        void IPointerPresentationTarget.PlayPointerPressPresentation()
        {
        }

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(_pressBindingPath))
            {
                _pressAction = new InputAction($"{name}_Press", InputActionType.Button, _pressBindingPath);
                _pressAction.performed += OnPressPerformed;
                _pressAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (_pressAction != null)
            {
                _pressAction.performed -= OnPressPerformed;
                _pressAction.Disable();
                _pressAction.Dispose();
                _pressAction = null;
            }
            CancelAllPresentation();
        }

        private void OnPressPerformed(InputAction.CallbackContext context)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || _camera == null || _interactionCollider == null)
                return;

            Vector2 screenPosition = pointer.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_interactionCollider.Raycast(ray, out _, _camera.farClipPlane))
                NotifyInteractionRequested();
        }

        private void StartHybrid(
            AnimatorTriggerPresentationBinding authored,
            ProceduralTransformPresentationBinding procedural,
            ref HybridOneShotPresentationRun run,
            Action onCompleted)
        {
            CancelRun(ref run);
            run = HybridOneShotPresentationRun.Start(this, authored, procedural, onCompleted);
        }

        private static void CancelRun(ref HybridOneShotPresentationRun run)
        {
            run?.Cancel();
            run = null;
        }
    }
}
