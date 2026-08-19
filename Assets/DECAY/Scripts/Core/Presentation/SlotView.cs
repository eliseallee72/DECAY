using System;
using UnityEngine;
using UnityEngine.Events;

namespace Decay
{
    /// <summary>
    /// Optional editor-authored presentation surface for one board slot. SlotView never owns occupancy,
    /// condition, score, or DECAY results; it only exposes named presentation requests for later authored visuals.
    /// </summary>
    public sealed class SlotView : MonoBehaviour
    {
        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _checkedPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _breakPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _unstablePresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _scorePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Score Value Presentation Hook")]
        [Tooltip("Connect an editor-authored score display component here. Pass 1 does not create text or sprites.")]
        [SerializeField] private UnityEvent<int> _showScoreValue = new UnityEvent<int>();
        [SerializeField] private UnityEvent _hideScoreValue = new UnityEvent();

        private Action _checkedCompletion;
        private Action _breakCompletion;
        private Action _unstableCompletion;
        private Action _scoreCompletion;

        internal void ShowScoreValue(int scoreValue) => _showScoreValue?.Invoke(scoreValue);
        internal void HideScoreValue() => _hideScoreValue?.Invoke();

        internal void PlayCheckedPresentation(Action onCompleted) => Start(_checkedPresentation, ref _checkedCompletion, onCompleted);
        internal void PlayBreakPresentation(Action onCompleted) => Start(_breakPresentation, ref _breakCompletion, onCompleted);
        internal void PlayUnstablePresentation(Action onCompleted) => Start(_unstablePresentation, ref _unstableCompletion, onCompleted);
        internal void PlayScorePresentation(Action onCompleted) => Start(_scorePresentation, ref _scoreCompletion, onCompleted);

        public void NotifyCheckedPresentationComplete() => Complete(ref _checkedCompletion);
        public void NotifyBreakPresentationComplete() => Complete(ref _breakCompletion);
        public void NotifyUnstablePresentationComplete() => Complete(ref _unstableCompletion);
        public void NotifyScorePresentationComplete() => Complete(ref _scoreCompletion);

        internal void CancelAllPresentation()
        {
            Cancel(_checkedPresentation, ref _checkedCompletion);
            Cancel(_breakPresentation, ref _breakCompletion);
            Cancel(_unstablePresentation, ref _unstableCompletion);
            Cancel(_scorePresentation, ref _scoreCompletion);
        }

        private void OnDisable()
        {
            CancelAllPresentation();
        }

        private static void Start(AnimatorTriggerPresentationBinding binding, ref Action pending, Action completed)
        {
            pending = null;
            if (!binding.Play())
            {
                completed?.Invoke();
                return;
            }
            pending = completed;
        }

        private static void Cancel(AnimatorTriggerPresentationBinding binding, ref Action pending)
        {
            pending = null;
            binding.Cancel();
        }

        private static void Complete(ref Action pending)
        {
            Action callback = pending;
            pending = null;
            callback?.Invoke();
        }
    }
}
