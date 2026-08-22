using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Generic editor-authored presentation surface for ambient battle objects such as the enemy or abacus.
    /// It owns no gameplay state. A caller may reflect an authoritative/presentation phase into Idle, while hover and
    /// decorative press feedback remain presentation-only. One shared Animator/Controller owns the authored visuals.
    /// </summary>
    public sealed class AmbientPresentationView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Animator")]
        [Tooltip("Single Animator used by this ambient object. If empty, the View auto-finds an Animator on this object or its children.")]
        [SerializeField] private Animator _animator;

        [Header("Persistent Presentation")]
        [SerializeField] private AnimatorBoolPresentationBinding _idlePresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();

        [Header("Optional Decorative Press")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();

        bool IPointerPresentationTarget.PointerPresentationEnabled => isActiveAndEnabled;

        public bool TryValidate(out string error)
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
            return _idlePresentation.TryValidate($"{name} Idle", out error)
                && _hoverPresentation.TryValidate($"{name} Hover", out error)
                && _decorativePressPresentation.TryValidate($"{name} Decorative Press", out error);
        }

        internal void SetIdlePresentation(bool isActive) => _idlePresentation.SetActive(isActive);

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) =>
            _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation() =>
            _decorativePressPresentation.Play();

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

        private void OnDisable()
        {
            _decorativePressPresentation.Cancel();
            _hoverPresentation.SetActive(false);
            _idlePresentation.SetActive(false);
        }

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
            _idlePresentation.BindAnimator(_animator);
            _hoverPresentation.BindAnimator(_animator);
            _decorativePressPresentation.BindAnimator(_animator);
        }
    }
}
