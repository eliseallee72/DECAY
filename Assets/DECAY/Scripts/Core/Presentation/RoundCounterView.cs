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

        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _showRoundPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetPresentation = new AnimatorTriggerPresentationBinding();
        [Tooltip("Optional presentation-only response for clicking the counter. It does not submit a gameplay request.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();

        [Header("Optional Procedural Layers")]
        [SerializeField] private ProceduralTransformPresentationBinding _showRoundMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _resetMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _decorativePressMotion = new ProceduralTransformPresentationBinding();

        private HybridOneShotPresentationRun _showRoundRun;
        private HybridOneShotPresentationRun _resetRun;
        private HybridOneShotPresentationRun _decorativePressRun;

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

        public bool TryValidate(out string error)
        {
            return _roundNumber.TryValidate($"{name} Round Number", out error)
                && _idlePresentation.TryValidate($"{name} Idle", out error)
                && _hoverPresentation.TryValidate($"{name} Hover", out error)
                && _reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                && _showRoundPresentation.TryValidate($"{name} Show Round", out error)
                && _resetPresentation.TryValidate($"{name} Reset", out error)
                && _decorativePressPresentation.TryValidate($"{name} Decorative Press", out error)
                && _showRoundMotion.TryValidate($"{name} Show Round Motion", out error)
                && _resetMotion.TryValidate($"{name} Reset Motion", out error)
                && _decorativePressMotion.TryValidate($"{name} Decorative Press Motion", out error);
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
            StartHybrid(_showRoundPresentation, _showRoundMotion, ref _showRoundRun, onCompleted);
        }

        internal void PlayReset(Action onCompleted) =>
            StartHybrid(_resetPresentation, _resetMotion, ref _resetRun, onCompleted);

        public void NotifyShowRoundPresentationComplete() => _showRoundRun?.NotifyAuthoredComplete();
        public void NotifyResetPresentationComplete() => _resetRun?.NotifyAuthoredComplete();
        public void NotifyDecorativePressPresentationComplete() => _decorativePressRun?.NotifyAuthoredComplete();

        internal void CancelAllPresentation()
        {
            CancelRun(ref _showRoundRun);
            CancelRun(ref _resetRun);
            CancelRun(ref _decorativePressRun);
            _hoverPresentation.SetActive(false);
            _idlePresentation.SetActive(false);
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) => _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation()
        {
            StartHybrid(
                _decorativePressPresentation,
                _decorativePressMotion,
                ref _decorativePressRun,
                null);
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
