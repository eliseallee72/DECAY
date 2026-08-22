using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored Round Counter presentation. BattleState owns the round number; this view only receives it.
    /// Hover and decorative press responses are presentation-only and never alter battle flow.
    /// </summary>
    public sealed class RoundCounterView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Persistent Presentation")]
        [SerializeField] private AnimatorIntPresentationBinding _roundNumber = new AnimatorIntPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _idlePresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Authored Round Counter One-Shots")]
        [SerializeField] private AnimatorTriggerPresentationBinding _showRoundPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetPresentation = new AnimatorTriggerPresentationBinding();
        [Tooltip("Optional presentation-only response for clicking the counter. It does not submit a gameplay request.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();

        private Action _showRoundCompletion;
        private Action _resetCompletion;

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

        public bool TryValidate(out string error)
        {
            return _roundNumber.TryValidate($"{name} Round Number", out error)
                && _idlePresentation.TryValidate($"{name} Idle", out error)
                && _hoverPresentation.TryValidate($"{name} Hover", out error)
                && _reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                && _showRoundPresentation.TryValidate($"{name} Show Round", out error)
                && _resetPresentation.TryValidate($"{name} Reset", out error)
                && _decorativePressPresentation.TryValidate($"{name} Decorative Press", out error);
        }

        internal void ReconcileRoundNumber(int roundNumber, bool invokeRecoveryHook = false)
        {
            _roundNumber.SetValue(roundNumber);
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void SetIdlePresentation(bool isActive) => _idlePresentation.SetActive(isActive);

        internal void PlayShowRound(int roundNumber, Action onCompleted)
        {
            _roundNumber.SetValue(roundNumber);
            StartAuthoredPresentation(_showRoundPresentation, ref _showRoundCompletion, onCompleted);
        }

        internal void PlayReset(Action onCompleted) =>
            StartAuthoredPresentation(_resetPresentation, ref _resetCompletion, onCompleted);

        public void NotifyShowRoundPresentationComplete() => CompleteOneShot(ref _showRoundCompletion);
        public void NotifyResetPresentationComplete() => CompleteOneShot(ref _resetCompletion);

        internal void CancelAllPresentation()
        {
            CancelOneShot(_showRoundPresentation, ref _showRoundCompletion);
            CancelOneShot(_resetPresentation, ref _resetCompletion);
            _decorativePressPresentation.Cancel();
            _hoverPresentation.SetActive(false);
            _idlePresentation.SetActive(false);
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) => _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation() => _decorativePressPresentation.Play();

        private void OnDisable() => CancelAllPresentation();

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
