using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Optional editor-authored Animator trigger binding. The owning View supplies one shared Animator for the object;
    /// this binding only stores parameter names. The Animator Controller and Animation Clips own state speed, curves,
    /// Transform animation, SpriteRenderer alpha/color/sorting order, Animator layers/blending, sprite swaps, and other
    /// Unity-animatable properties. None of those visual choices are hard-coded here.
    /// </summary>
    [Serializable]
    public sealed class AnimatorTriggerPresentationBinding
    {
        [SerializeField] private string _playTrigger;
        [SerializeField] private string _cancelTrigger;

        [NonSerialized] private Animator _animator;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_playTrigger);

        internal void BindAnimator(Animator animator) => _animator = animator;

        public bool TryValidate(string label, out string error)
        {
            bool hasPlay = !string.IsNullOrWhiteSpace(_playTrigger);
            bool hasCancel = !string.IsNullOrWhiteSpace(_cancelTrigger);
            if (!hasPlay && !hasCancel)
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: this presentation uses Animator parameters, but the owning View has no Animator assigned/found.";
                return false;
            }

            if (!hasPlay)
            {
                error = $"{label}: Play Trigger is required when a Cancel Trigger is configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal bool Play()
        {
            if (!IsConfigured)
                return false;

            _animator.SetTrigger(Animator.StringToHash(_playTrigger));
            return true;
        }

        internal void Cancel()
        {
            if (_animator == null)
                return;

            if (!string.IsNullOrWhiteSpace(_playTrigger))
                _animator.ResetTrigger(Animator.StringToHash(_playTrigger));

            if (!string.IsNullOrWhiteSpace(_cancelTrigger))
                _animator.SetTrigger(Animator.StringToHash(_cancelTrigger));
        }
    }

    /// <summary>
    /// Optional editor-authored persistent Animator state. The owning View supplies one shared Animator for the object;
    /// this binding stores only the bool parameter name. Gameplay rules remain authoritative elsewhere.
    /// </summary>
    [Serializable]
    public sealed class AnimatorBoolPresentationBinding
    {
        [SerializeField] private string _boolParameter;

        [NonSerialized] private Animator _animator;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_boolParameter);

        internal void BindAnimator(Animator animator) => _animator = animator;

        public bool TryValidate(string label, out string error)
        {
            if (string.IsNullOrWhiteSpace(_boolParameter))
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: this presentation uses an Animator bool, but the owning View has no Animator assigned/found.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void SetActive(bool isActive)
        {
            if (IsConfigured)
                _animator.SetBool(Animator.StringToHash(_boolParameter), isActive);
        }
    }

    /// <summary>
    /// Optional editor-authored integer Animator parameter, useful when one Animator owns a family of authored states.
    /// The owning View supplies the shared Animator; this binding stores only the parameter name.
    /// </summary>
    [Serializable]
    public sealed class AnimatorIntPresentationBinding
    {
        [SerializeField] private string _intParameter;

        [NonSerialized] private Animator _animator;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_intParameter);

        internal void BindAnimator(Animator animator) => _animator = animator;

        public bool TryValidate(string label, out string error)
        {
            if (string.IsNullOrWhiteSpace(_intParameter))
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: this presentation uses an Animator int, but the owning View has no Animator assigned/found.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void SetValue(int value)
        {
            if (IsConfigured)
                _animator.SetInteger(Animator.StringToHash(_intParameter), value);
        }
    }
}
