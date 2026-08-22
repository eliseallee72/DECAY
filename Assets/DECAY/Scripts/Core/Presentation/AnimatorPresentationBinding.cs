using System;
using UnityEngine;

namespace Decay
{
    internal static class AnimatorPresentationParameterValidation
    {
        internal static bool TryValidate(
            Animator animator,
            string parameterName,
            AnimatorControllerParameterType expectedType,
            string label,
            out string error)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                error = string.Empty;
                return true;
            }

            if (animator == null)
            {
                error = $"{label}: this presentation uses an Animator parameter, but the owning View has no Animator assigned/found.";
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (string.Equals(parameter.name, parameterName, StringComparison.Ordinal))
                {
                    if (parameter.type == expectedType)
                    {
                        error = string.Empty;
                        return true;
                    }

                    error = $"{label}: Animator parameter '{parameterName}' exists but is {parameter.type}, not {expectedType}.";
                    return false;
                }
            }

            error = $"{label}: Animator parameter '{parameterName}' no longer exists on the assigned Animator Controller.";
            return false;
        }
    }

    /// <summary>
    /// Optional editor-authored Animator trigger binding. The owning View supplies one shared Animator for the object;
    /// this binding stores the controller parameter selected in the Unity Inspector. The Animator Controller and
    /// Animation Clips own state speed, transitions/exits, curves, Transform animation, SpriteRenderer properties,
    /// layers/blending, sprite swaps, and other Unity-animatable properties.
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

            if (!hasPlay)
            {
                error = $"{label}: a Play Trigger is required when a Cancel Trigger is selected.";
                return false;
            }

            if (!AnimatorPresentationParameterValidation.TryValidate(
                    _animator,
                    _playTrigger,
                    AnimatorControllerParameterType.Trigger,
                    $"{label} Play Trigger",
                    out error))
            {
                return false;
            }

            if (hasCancel && !AnimatorPresentationParameterValidation.TryValidate(
                    _animator,
                    _cancelTrigger,
                    AnimatorControllerParameterType.Trigger,
                    $"{label} Cancel Trigger",
                    out error))
            {
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
    /// this binding stores the Bool parameter selected from that Animator Controller. Gameplay rules remain authoritative.
    /// </summary>
    [Serializable]
    public sealed class AnimatorBoolPresentationBinding
    {
        [SerializeField] private string _boolParameter;

        [NonSerialized] private Animator _animator;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_boolParameter);

        internal void BindAnimator(Animator animator) => _animator = animator;

        public bool TryValidate(string label, out string error) =>
            AnimatorPresentationParameterValidation.TryValidate(
                _animator,
                _boolParameter,
                AnimatorControllerParameterType.Bool,
                label,
                out error);

        internal void SetActive(bool isActive)
        {
            if (IsConfigured)
                _animator.SetBool(Animator.StringToHash(_boolParameter), isActive);
        }
    }

    /// <summary>
    /// Optional editor-authored integer Animator parameter, useful when one Animator owns a family of authored states.
    /// The owning View supplies the shared Animator; this binding stores the Int parameter selected from its controller.
    /// </summary>
    [Serializable]
    public sealed class AnimatorIntPresentationBinding
    {
        [SerializeField] private string _intParameter;

        [NonSerialized] private Animator _animator;

        public bool IsConfigured => _animator != null && !string.IsNullOrWhiteSpace(_intParameter);

        internal void BindAnimator(Animator animator) => _animator = animator;

        public bool TryValidate(string label, out string error) =>
            AnimatorPresentationParameterValidation.TryValidate(
                _animator,
                _intParameter,
                AnimatorControllerParameterType.Int,
                label,
                out error);

        internal void SetValue(int value)
        {
            if (IsConfigured)
                _animator.SetInteger(Animator.StringToHash(_intParameter), value);
        }
    }
}
