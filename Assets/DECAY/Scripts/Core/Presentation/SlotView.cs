using System;
using UnityEngine;
using UnityEngine.Events;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation surface for one board slot. BoardState owns occupancy/condition; SlotView receives
    /// the authoritative condition and exposes transition hooks without making animation responsible for final state.
    /// </summary>
    public sealed class SlotView : MonoBehaviour
    {
        [Header("Persistent Authoritative Presentation")]
        [Tooltip("Animator int representing the authoritative SlotCondition. The Animator decides how each value looks.")]
        [SerializeField] private AnimatorIntPresentationBinding _conditionPresentation = new AnimatorIntPresentationBinding();
        [Tooltip("Optional editor-authored trigger used after interruption/reconciliation to return to the persistent authoritative visual state.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _checkedPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _breakPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _unstablePresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _scorePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Optional Procedural Layers")]
        [SerializeField] private ProceduralTransformPresentationBinding _checkedMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _breakMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _unstableMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _scoreMotion = new ProceduralTransformPresentationBinding();

        [Header("Score Value Presentation Hook")]
        [Tooltip("Connect an editor-authored score display component here. Framework code does not create text or sprites.")]
        [SerializeField] private UnityEvent<int> _showScoreValue = new UnityEvent<int>();
        [SerializeField] private UnityEvent _hideScoreValue = new UnityEvent();

        private HybridOneShotPresentationRun _checkedRun;
        private HybridOneShotPresentationRun _breakRun;
        private HybridOneShotPresentationRun _unstableRun;
        private HybridOneShotPresentationRun _scoreRun;

        public bool TryValidate(out string error)
        {
            return _conditionPresentation.TryValidate($"{name} Slot Condition", out error)
                && _reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                && _checkedPresentation.TryValidate($"{name} Checked", out error)
                && _breakPresentation.TryValidate($"{name} Break", out error)
                && _unstablePresentation.TryValidate($"{name} Unstable", out error)
                && _scorePresentation.TryValidate($"{name} Score", out error)
                && _checkedMotion.TryValidate($"{name} Checked Motion", out error)
                && _breakMotion.TryValidate($"{name} Break Motion", out error)
                && _unstableMotion.TryValidate($"{name} Unstable Motion", out error)
                && _scoreMotion.TryValidate($"{name} Score Motion", out error);
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
            StartHybrid(_checkedPresentation, _checkedMotion, ref _checkedRun, onCompleted);
        internal void PlayBreakPresentation(Action onCompleted) =>
            StartHybrid(_breakPresentation, _breakMotion, ref _breakRun, onCompleted);
        internal void PlayUnstablePresentation(Action onCompleted) =>
            StartHybrid(_unstablePresentation, _unstableMotion, ref _unstableRun, onCompleted);
        internal void PlayScorePresentation(Action onCompleted) =>
            StartHybrid(_scorePresentation, _scoreMotion, ref _scoreRun, onCompleted);

        public void NotifyCheckedPresentationComplete() => _checkedRun?.NotifyAuthoredComplete();
        public void NotifyBreakPresentationComplete() => _breakRun?.NotifyAuthoredComplete();
        public void NotifyUnstablePresentationComplete() => _unstableRun?.NotifyAuthoredComplete();
        public void NotifyScorePresentationComplete() => _scoreRun?.NotifyAuthoredComplete();

        internal void CancelAllPresentation()
        {
            CancelRun(ref _checkedRun);
            CancelRun(ref _breakRun);
            CancelRun(ref _unstableRun);
            CancelRun(ref _scoreRun);
        }

        private void OnDisable() => CancelAllPresentation();

        private void StartHybrid(
            AnimatorTriggerPresentationBinding authored,
            ProceduralTransformPresentationBinding procedural,
            ref HybridOneShotPresentationRun run,
            Action onCompleted)
        {
            CancelRun(ref run);
            run = HybridOneShotPresentationRun.Start(this, authored, procedural, onCompleted);
        }

        private static void CancelRun(ref HybridOneShotPresentationRun run)
        {
            run?.Cancel();
            run = null;
        }
    }
}
