using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Decay
{
    /// <summary>
    /// Hourglass presentation/input surface. It collects pointer input and submits requests through the battle flow.
    /// Authored hourglass visuals are editor-assigned Animator bindings; this View never changes gameplay state directly.
    /// </summary>
    public sealed class HourglassView : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Collider _interactionCollider;
        [SerializeField] private BattleCompositionRoot _compositionRoot;

        [Header("Authored Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _rollToRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _decayPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetToSetupPresentation = new AnimatorTriggerPresentationBinding();

        private BattlePresentationDirector _presentationDirector;
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

            if (_compositionRoot == null)
            {
                error = $"{name}: HourglassView requires a BattleCompositionRoot reference.";
                return false;
            }

            if (!_rollToRepositionPresentation.TryValidate($"{name} Roll Presentation", out error)
                || !_decayPresentation.TryValidate($"{name} Decay Presentation", out error)
                || !_resetToSetupPresentation.TryValidate($"{name} Reset Presentation", out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        public BattleFlowResult RequestRoll()
        {
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
            return _compositionRoot.RequestRoll();
        }

        public BattleFlowResult RequestDecay()
        {
            if (!TryValidate(out string error))
                throw new InvalidOperationException(error);
            return _compositionRoot.RequestDecay();
        }

        internal void BindPresentationDirector(BattlePresentationDirector presentationDirector)
        {
            _presentationDirector = presentationDirector ?? throw new ArgumentNullException(nameof(presentationDirector));
        }

        internal void UnbindPresentationDirector(BattlePresentationDirector presentationDirector)
        {
            if (_presentationDirector == presentationDirector)
                _presentationDirector = null;
        }

        internal void PlayRollPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_rollToRepositionPresentation, ref _rollCompletion, onCompleted);

        internal void PlayDecayPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_decayPresentation, ref _decayCompletion, onCompleted);

        internal void PlayResetPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_resetToSetupPresentation, ref _resetCompletion, onCompleted);

        /// <summary>
        /// Animation Event endpoint for the authored Roll-to-Reposition presentation. When no director-owned
        /// presentation is active, this preserves the existing explicit completion hook used by tests/fallback flow.
        /// </summary>
        public void NotifyRollPresentationComplete()
        {
            if (_rollCompletion != null)
            {
                CompleteOneShot(ref _rollCompletion);
                return;
            }

            if (_presentationDirector == null && _compositionRoot != null && _compositionRoot.IsInitialized)
                _compositionRoot.CompleteRoll();
        }

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

        internal void AdvanceDecayImmediatelyToNextPlayableState()
        {
            BattleFlowResult decay = RequestDecay();
            if (!decay.IsApproved)
                return;

            BattleFlowResult decayCompletion = _compositionRoot.CompleteDecay();
            if (!decayCompletion.IsApproved)
                return;

            BattleFlowResult scoreCompletion = _compositionRoot.CompleteScore();
            if (!scoreCompletion.IsApproved)
                return;

            BattleFlowResult roundCompletion = _compositionRoot.CompleteRoundEnd();
            if (!roundCompletion.IsApproved)
                return;

            if (_compositionRoot.Runtime.BattleState.CurrentPhase == BattlePhase.GameEnd)
                _compositionRoot.CompleteGameEnd();
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
                return;

            Ray ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!_interactionCollider.Raycast(ray, out _, _camera.farClipPlane))
                return;

            if (phase == BattlePhase.Setup)
            {
                if (_presentationDirector != null)
                    _presentationDirector.RequestRollFromHourglass();
                else
                    AdvanceRollImmediatelyToPlayerReposition();
                return;
            }

            if (_presentationDirector != null)
                _presentationDirector.RequestDecayFromHourglass();
            else
                AdvanceDecayImmediatelyToNextPlayableState();
        }

        private void AdvanceRollImmediatelyToPlayerReposition()
        {
            BattleFlowResult roll = RequestRoll();
            if (!roll.IsApproved)
                return;

            BattleFlowResult rollCompletion = _compositionRoot.CompleteRoll();
            if (!rollCompletion.IsApproved)
                return;

            _compositionRoot.CompleteEnemyReposition();
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

        private void OnDisable()
        {
            CancelAllPresentation();
        }
    }
}
