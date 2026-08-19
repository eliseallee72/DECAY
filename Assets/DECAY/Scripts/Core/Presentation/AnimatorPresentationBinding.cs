using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Optional editor-authored Animator trigger binding. Code requests a named presentation;
    /// the Animator Controller and Animation Clips own how that presentation looks and how long it lasts.
    /// </summary>
    [Serializable]
    public sealed class AnimatorTriggerPresentationBinding
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _playTrigger;
        [SerializeField] private string _cancelTrigger;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_playTrigger);

        public bool TryValidate(string label, out string error)
        {
            if (_animator == null && string.IsNullOrWhiteSpace(_playTrigger) && string.IsNullOrWhiteSpace(_cancelTrigger))
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: Animator is required when presentation parameters are configured.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_playTrigger))
            {
                error = $"{label}: Play Trigger is required when an Animator is configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal bool Play()
        {
            if (!IsConfigured)
            {
                return false;
            }

            _animator.SetTrigger(Animator.StringToHash(_playTrigger));
            return true;
        }

        internal void Cancel()
        {
            if (_animator == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_playTrigger))
            {
                _animator.ResetTrigger(Animator.StringToHash(_playTrigger));
            }

            if (!string.IsNullOrWhiteSpace(_cancelTrigger))
            {
                _animator.SetTrigger(Animator.StringToHash(_cancelTrigger));
            }
        }
    }

    /// <summary>
    /// Optional editor-authored persistent Animator state. The supplied bool is presentation state only;
    /// gameplay rules remain authoritative elsewhere.
    /// </summary>
    [Serializable]
    public sealed class AnimatorBoolPresentationBinding
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _boolParameter;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_boolParameter);

        public bool TryValidate(string label, out string error)
        {
            if (_animator == null && string.IsNullOrWhiteSpace(_boolParameter))
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: Animator is required when a bool parameter is configured.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_boolParameter))
            {
                error = $"{label}: Bool Parameter is required when an Animator is configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void SetActive(bool isActive)
        {
            if (!IsConfigured)
            {
                return;
            }

            _animator.SetBool(Animator.StringToHash(_boolParameter), isActive);
        }
    }
    /// <summary>
    /// Optional editor-authored integer Animator parameter, useful when one Animator owns a family of authored states.
    /// </summary>
    [Serializable]
    public sealed class AnimatorIntPresentationBinding
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _intParameter;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_intParameter);

        public bool TryValidate(string label, out string error)
        {
            if (_animator == null && string.IsNullOrWhiteSpace(_intParameter))
            {
                error = string.Empty;
                return true;
            }

            if (_animator == null)
            {
                error = $"{label}: Animator is required when an int parameter is configured.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_intParameter))
            {
                error = $"{label}: Int Parameter is required when an Animator is configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal void SetValue(int value)
        {
            if (!IsConfigured)
            {
                return;
            }

            _animator.SetInteger(Animator.StringToHash(_intParameter), value);
        }
    }

}
