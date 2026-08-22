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
        [Header("Animator")]
        [Tooltip("Single Animator used by this board presentation surface. If empty, the View auto-finds an Animator on this object or its children.")]
        [SerializeField] private Animator _animator;

        [SerializeField] private AnimatorTriggerPresentationBinding _enemyRepositionPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();
        private Action _enemyRepositionCompletion;

        public bool TryValidate(out string error)
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
            return _enemyRepositionPresentation.TryValidate($"{name} Enemy Reposition", out error)
                && _reconcilePresentation.TryValidate($"{name} Reconcile", out error);
        }

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

        private void Awake()
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
        }

        private void OnValidate()
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
        }

        private void OnDisable() => CancelAllPresentation();

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
            _enemyRepositionPresentation.BindAnimator(_animator);
            _reconcilePresentation.BindAnimator(_animator);
        }
    }
}
