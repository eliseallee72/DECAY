using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass input/presentation surface. Pointer input only raises an interaction request; authoritative battle flow
    /// decides what that request means. Authored Animator state then reflects the authoritative phase/result.
    /// </summary>
    public sealed class HourglassView : MonoBehaviour
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

        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _rollToRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _decayPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetToSetupPresentation = new AnimatorTriggerPresentationBinding();

        private InputAction _pressAction;
        private Action _interactionRequested;
        private Action _rollCompletion;
        private Action _decayCompletion;
        private Action _resetCompletion;

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
                || !_rollToRepositionPresentation.TryValidate($"{name} Roll Presentation", out error)
                || !_decayPresentation.TryValidate($"{name} Decay Presentation", out error)
                || !_resetToSetupPresentation.TryValidate($"{name} Reset Presentation", out error))
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
            StartAuthoredPresentation(_rollToRepositionPresentation, ref _rollCompletion, onCompleted);

        internal void PlayDecayPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_decayPresentation, ref _decayCompletion, onCompleted);

        internal void PlayResetPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_resetToSetupPresentation, ref _resetCompletion, onCompleted);

        /// <summary>
        /// Public editor/test endpoint for any alternate input surface. It only requests interaction; it never advances flow.
        /// </summary>
        public void NotifyInteractionRequested() => _interactionRequested?.Invoke();

        public void NotifyRollPresentationComplete() => CompleteOneShot(ref _rollCompletion);
        public void NotifyDecayPresentationComplete() => CompleteOneShot(ref _decayCompletion);
        public void NotifyResetPresentationComplete() => CompleteOneShot(ref _resetCompletion);

        internal void CancelRollPresentation()
        {
            _rollCompletion = null;
            _rollToRepositionPresentation.Cancel();
        }

        internal void CancelAllPresentation()
        {
            CancelRollPresentation();
            CancelOneShot(_decayPresentation, ref _decayCompletion);
            CancelOneShot(_resetToSetupPresentation, ref _resetCompletion);
        }

        internal void ConfigureForTests(Camera camera, Collider interactionCollider)
        {
            _camera = camera;
            _interactionCollider = interactionCollider;
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

        private static void StartAuthoredPresentation(
            AnimatorTriggerPresentationBinding binding,
            ref Action pendingCompletion,
            Action onCompleted)
        {
            pendingCompletion = null;
            if (!binding.Play())
            {
                onCompleted?.Invoke();
                return;
            }
            pendingCompletion = onCompleted;
        }

        private static void CancelOneShot(AnimatorTriggerPresentationBinding binding, ref Action pendingCompletion)
        {
            pendingCompletion = null;
            binding.Cancel();
        }

        private static void CompleteOneShot(ref Action pendingCompletion)
        {
            Action callback = pendingCompletion;
            pendingCompletion = null;
            callback?.Invoke();
        }
    }
}
