using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass input/presentation surface. Pointer input only raises an interaction request; authoritative battle flow
    /// decides what that request means. One shared Animator on this View owns the hourglass's authored 2D/3D visual
    /// timing and properties; individual presentation entries only name Animator parameters.
    /// </summary>
    public sealed class HourglassView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Event-Driven Pointer Input")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Collider _interactionCollider;
        [Tooltip("Input System binding used to request interaction. Stored as editor data rather than polled in Update.")]
        [SerializeField] private string _pressBindingPath = "<Mouse>/leftButton";

        [Header("Animator")]
        [Tooltip("Single Animator used by this HourglassView. If empty, the View auto-finds an Animator on this object or its children. Assign your own Animator Controller to that Animator component.")]
        [SerializeField] private Animator _animator;

        [Header("Persistent Authoritative Presentation")]
        [Tooltip("Animator int representing the authoritative BattlePhase. Animation state never establishes the phase.")]
        [SerializeField] private AnimatorIntPresentationBinding _phasePresentation = new AnimatorIntPresentationBinding();
        [Tooltip("Optional trigger used after cancellation/reconciliation to return to the persistent phase state.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();
        [Tooltip("Presentation-only hover state. This does not imply the hourglass interaction will be accepted.")]
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();

        [Header("Authored Hourglass One-Shots")]
        [SerializeField] private AnimatorTriggerPresentationBinding _rollToRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetToSetupPresentation = new AnimatorTriggerPresentationBinding();

        private InputAction _pressAction;
        private Action _interactionRequested;
        private Action _rollCompletion;
        private Action _resetCompletion;
        private bool _presentationInteractionEnabled = true;

        bool IPointerPresentationTarget.PointerPresentationEnabled =>
            isActiveAndEnabled && _presentationInteractionEnabled;

        public bool TryValidate(out string error)
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();

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

        /// <summary>
        /// Presentation/input-surface availability derived from authoritative battle flow. This never approves an action;
        /// the battle controller remains the authority when a request is submitted.
        /// </summary>
        internal void SetPresentationInteractionEnabled(bool isEnabled)
        {
            _presentationInteractionEnabled = isEnabled;
            if (!isEnabled)
                _hoverPresentation.SetActive(false);
        }

        internal void PlayRollPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_rollToRepositionPresentation, ref _rollCompletion, onCompleted);

        internal void PlayResetPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_resetToSetupPresentation, ref _resetCompletion, onCompleted);

        /// <summary>
        /// Public editor/test endpoint for any alternate input surface. It only requests interaction; it never advances flow.
        /// </summary>
        public void NotifyInteractionRequested()
        {
            if (_presentationInteractionEnabled)
                _interactionRequested?.Invoke();
        }

        public void NotifyRollPresentationComplete() => CompleteOneShot(ref _rollCompletion);
        public void NotifyResetPresentationComplete() => CompleteOneShot(ref _resetCompletion);

        internal void CancelRollPresentation()
        {
            _rollCompletion = null;
            _rollToRepositionPresentation.Cancel();
        }

        internal void CancelAllPresentation()
        {
            CancelRollPresentation();
            CancelOneShot(_resetToSetupPresentation, ref _resetCompletion);
            _hoverPresentation.SetActive(false);
        }

        internal void ConfigureForTests(Camera camera, Collider interactionCollider, Animator animator = null)
        {
            _camera = camera;
            _interactionCollider = interactionCollider;
            if (animator != null)
                _animator = animator;
            BindPresentationAnimator();
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered)
        {
            if (_presentationInteractionEnabled)
                _hoverPresentation.SetActive(isHovered);
            else if (isHovered)
                _hoverPresentation.SetActive(false);
        }

        // The hourglass already has a functional click request. Decorative press feedback is intentionally not duplicated here.
        void IPointerPresentationTarget.PlayPointerPressPresentation()
        {
        }

        private void Awake()
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
        }

        private void OnEnable()
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();

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

        private void OnValidate()
        {
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
            ResolveAnimatorReference();
            BindPresentationAnimator();
        }

        private void OnPressPerformed(InputAction.CallbackContext context)
        {
            if (!_presentationInteractionEnabled)
                return;

            Pointer pointer = Pointer.current;
            if (pointer == null || _camera == null || _interactionCollider == null)
                return;

            Vector2 screenPosition = pointer.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);
            if (_interactionCollider.Raycast(ray, out _, _camera.farClipPlane))
                NotifyInteractionRequested();
        }

        private void ResolveAnimatorReference()
        {
            if (_animator != null)
                return;

            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void BindPresentationAnimator()
        {
            _phasePresentation.BindAnimator(_animator);
            _reconcilePresentation.BindAnimator(_animator);
            _hoverPresentation.BindAnimator(_animator);
            _rollToRepositionPresentation.BindAnimator(_animator);
            _resetToSetupPresentation.BindAnimator(_animator);
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
