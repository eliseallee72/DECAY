using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Generic editor-authored presentation surface for ambient battle objects such as the enemy or abacus.
    /// It owns no gameplay state. A caller may reflect an authoritative/presentation phase into Idle, while hover and
    /// decorative press feedback remain presentation-only. Leaving every binding empty is valid.
    /// </summary>
    public sealed class AmbientPresentationView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Persistent Presentation")]
        [SerializeField] private AnimatorBoolPresentationBinding _idlePresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();

        [Header("Optional Decorative Press")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _decorativePressMotion = new ProceduralTransformPresentationBinding();

        private HybridOneShotPresentationRun _decorativePressRun;

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

        public bool TryValidate(out string error) =>
            _idlePresentation.TryValidate($"{name} Idle", out error)
            && _hoverPresentation.TryValidate($"{name} Hover", out error)
            && _decorativePressPresentation.TryValidate($"{name} Decorative Press", out error)
            && _decorativePressMotion.TryValidate($"{name} Decorative Press Motion", out error);

        internal void SetIdlePresentation(bool isActive) => _idlePresentation.SetActive(isActive);

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) =>
            _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation()
        {
            _decorativePressRun?.Cancel();
            _decorativePressRun = HybridOneShotPresentationRun.Start(
                this,
                _decorativePressPresentation,
                _decorativePressMotion,
                null);
        }

        public void NotifyDecorativePressPresentationComplete() =>
            _decorativePressRun?.NotifyAuthoredComplete();

        private void OnDisable()
        {
            _decorativePressRun?.Cancel();
            _decorativePressRun = null;
            _hoverPresentation.SetActive(false);
            _idlePresentation.SetActive(false);
        }
    }
}
