using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored board-wide presentation surface. Enemy movement authority remains outside this View;
    /// this View only exposes the authored cue/completion seam.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] private AnimatorTriggerPresentationBinding _enemyRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();
        private Action _enemyRepositionCompletion;

        public bool TryValidate(out string error) =>
            _enemyRepositionPresentation.TryValidate($"{name} Enemy Reposition", out error)
            && _reconcilePresentation.TryValidate($"{name} Reconcile", out error);

        internal void ReconcileAuthoritativePresentation(bool invokeRecoveryHook)
        {
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void PlayEnemyRepositionPresentation(Action onCompleted)
        {
            _enemyRepositionCompletion = null;
            if (!_enemyRepositionPresentation.Play())
            {
                onCompleted?.Invoke();
                return;
            }
            _enemyRepositionCompletion = onCompleted;
        }

        public void NotifyEnemyRepositionPresentationComplete()
        {
            Action callback = _enemyRepositionCompletion;
            _enemyRepositionCompletion = null;
            callback?.Invoke();
        }

        internal void CancelAllPresentation()
        {
            _enemyRepositionCompletion = null;
            _enemyRepositionPresentation.Cancel();
        }

        private void OnDisable() => CancelAllPresentation();
    }
}
