using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored board-wide presentation surface. Enemy movement authority remains outside this View;
    /// authored and optional procedural cue layers only communicate that authoritative activity.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] private AnimatorTriggerPresentationBinding _enemyRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _enemyRepositionMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();
        private HybridOneShotPresentationRun _enemyRepositionRun;

        public bool TryValidate(out string error) =>
            _enemyRepositionPresentation.TryValidate($"{name} Enemy Reposition", out error)
            && _enemyRepositionMotion.TryValidate($"{name} Enemy Reposition Motion", out error)
            && _reconcilePresentation.TryValidate($"{name} Reconcile", out error);

        internal void ReconcileAuthoritativePresentation(bool invokeRecoveryHook)
        {
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void PlayEnemyRepositionPresentation(Action onCompleted)
        {
            CancelAllPresentation();
            _enemyRepositionRun = HybridOneShotPresentationRun.Start(
                this,
                _enemyRepositionPresentation,
                _enemyRepositionMotion,
                onCompleted);
        }

        public void NotifyEnemyRepositionPresentationComplete() =>
            _enemyRepositionRun?.NotifyAuthoredComplete();

        internal void CancelAllPresentation()
        {
            _enemyRepositionRun?.Cancel();
            _enemyRepositionRun = null;
        }

        private void OnDisable() => CancelAllPresentation();
    }
}
