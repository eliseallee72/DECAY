using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored Round Counter presentation. BattleState owns the round number; this view only receives it.
    /// </summary>
    public sealed class RoundCounterView : MonoBehaviour
    {
        [SerializeField] private AnimatorIntPresentationBinding _roundNumber = new AnimatorIntPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _showRoundPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetPresentation = new AnimatorTriggerPresentationBinding();

        private Action _showRoundCompletion;
        private Action _resetCompletion;

        public bool TryValidate(out string error)
        {
            return _roundNumber.TryValidate($"{name} Round Number", out error)
                && _showRoundPresentation.TryValidate($"{name} Show Round", out error)
                && _resetPresentation.TryValidate($"{name} Reset", out error);
        }

        internal void PlayShowRound(int roundNumber, Action onCompleted)
        {
            _roundNumber.SetValue(roundNumber);
            Start(_showRoundPresentation, ref _showRoundCompletion, onCompleted);
        }

        internal void PlayReset(Action onCompleted)
        {
            Start(_resetPresentation, ref _resetCompletion, onCompleted);
        }

        public void NotifyShowRoundPresentationComplete() => Complete(ref _showRoundCompletion);
        public void NotifyResetPresentationComplete() => Complete(ref _resetCompletion);

        internal void CancelAllPresentation()
        {
            _showRoundCompletion = null;
            _resetCompletion = null;
            _showRoundPresentation.Cancel();
            _resetPresentation.Cancel();
        }

        private void OnDisable() => CancelAllPresentation();

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

        private static void Complete(ref Action pending)
        {
            Action callback = pending;
            pending = null;
            callback?.Invoke();
        }
    }
}
