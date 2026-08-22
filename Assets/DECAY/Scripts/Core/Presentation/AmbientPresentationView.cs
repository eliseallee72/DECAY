using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Generic editor-authored presentation surface for ambient battle objects such as the enemy or abacus.
    /// It owns no gameplay state. A caller may reflect an authoritative/presentation phase into Idle, while hover and
    /// decorative press feedback remain presentation-only. The attached Animator/Animation Clips own how those visuals look.
    /// </summary>
    public sealed class AmbientPresentationView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Persistent Presentation")]
        [SerializeField] private AnimatorBoolPresentationBinding _idlePresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();

        [Header("Optional Decorative Press")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

        public bool TryValidate(out string error) =>
            _idlePresentation.TryValidate($"{name} Idle", out error)
            && _hoverPresentation.TryValidate($"{name} Hover", out error)
            && _decorativePressPresentation.TryValidate($"{name} Decorative Press", out error);

        internal void SetIdlePresentation(bool isActive) => _idlePresentation.SetActive(isActive);

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) =>
            _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation() =>
            _decorativePressPresentation.Play();

        private void OnDisable()
        {
            _decorativePressPresentation.Cancel();
            _hoverPresentation.SetActive(false);
            _idlePresentation.SetActive(false);
        }
    }
}
