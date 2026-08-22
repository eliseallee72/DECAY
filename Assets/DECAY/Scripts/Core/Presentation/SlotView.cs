using System;
using UnityEngine;
using UnityEngine.Events;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation surface for one board slot. BoardState owns occupancy/condition; SlotView receives
    /// the authoritative condition and exposes only the slot's actual authored transition hooks.
    /// </summary>
    public sealed class SlotView : MonoBehaviour
    {
        [Header("Persistent Authoritative Presentation")]
        [Tooltip("Animator int representing the authoritative SlotCondition. The Animator decides how each value looks.")]
        [SerializeField] private AnimatorIntPresentationBinding _conditionPresentation = new AnimatorIntPresentationBinding();
        [Tooltip("Optional editor-authored trigger used after interruption/reconciliation to return to the persistent authoritative visual state.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Authored Slot One-Shots")]
        [SerializeField] private AnimatorTriggerPresentationBinding _checkedPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _breakPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _unstablePresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _scorePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Score Value Presentation Hook")]
        [Tooltip("Connect an editor-authored score display component here. Framework code does not create text or sprites.")]
        [SerializeField] private UnityEvent<int> _showScoreValue = new UnityEvent<int>();
        [SerializeField] private UnityEvent _hideScoreValue = new UnityEvent();

        private Action _checkedCompletion;
        private Action _breakCompletion;
        private Action _unstableCompletion;
        private Action _scoreCompletion;

        public bool TryValidate(out string error)
        {
            return _conditionPresentation.TryValidate($"{name} Slot Condition", out error)
                && _reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                && _checkedPresentation.TryValidate($"{name} Checked", out error)
                && _breakPresentation.TryValidate($"{name} Break", out error)
                && _unstablePresentation.TryValidate($"{name} Unstable", out error)
                && _scorePresentation.TryValidate($"{name} Score", out error);
        }

        internal void ReconcileCondition(SlotCondition condition, bool invokeRecoveryHook)
        {
            _conditionPresentation.SetValue((int)condition);
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void ShowScoreValue(int scoreValue) => _showScoreValue?.Invoke(scoreValue);
        internal void HideScoreValue() => _hideScoreValue?.Invoke();

        internal void PlayCheckedPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_checkedPresentation, ref _checkedCompletion, onCompleted);
        internal void PlayBreakPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_breakPresentation, ref _breakCompletion, onCompleted);
        internal void PlayUnstablePresentation(Action onCompleted) =>
            StartAuthoredPresentation(_unstablePresentation, ref _unstableCompletion, onCompleted);
        internal void PlayScorePresentation(Action onCompleted) =>
            StartAuthoredPresentation(_scorePresentation, ref _scoreCompletion, onCompleted);

        public void NotifyCheckedPresentationComplete() => CompleteOneShot(ref _checkedCompletion);
        public void NotifyBreakPresentationComplete() => CompleteOneShot(ref _breakCompletion);
        public void NotifyUnstablePresentationComplete() => CompleteOneShot(ref _unstableCompletion);
        public void NotifyScorePresentationComplete() => CompleteOneShot(ref _scoreCompletion);

        internal void CancelAllPresentation()
        {
            CancelOneShot(_checkedPresentation, ref _checkedCompletion);
            CancelOneShot(_breakPresentation, ref _breakCompletion);
            CancelOneShot(_unstablePresentation, ref _unstableCompletion);
            CancelOneShot(_scorePresentation, ref _scoreCompletion);
        }

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
