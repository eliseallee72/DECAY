using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Structural presentation channels for known effect contexts. This describes where an authored effect visual
    /// participates; it does not describe gameplay trigger rules or effect priority.
    /// </summary>
    public enum PresentationChannel
    {
        Default = 0,
        PredictiveIdle = 1,
        DecayProcess = 2,
        ScoreProcess = 3
    }

    /// <summary>
    /// Typed presentation request for one already-authoritative effect occurrence. Presentation may use the stable
    /// effect identity and source/target context to select authored visuals, but never interprets the effect rules.
    /// </summary>
    internal readonly struct EffectPresentationRequest
    {
        internal EffectPresentationRequest(
            EffectId effectId,
            PresentationChannel channel,
            EffectInstanceId effectInstanceId = default,
            DiceInstanceId sourceDiceId = default,
            DiceInstanceId targetDiceId = default)
        {
            if (!effectId.IsValid)
                throw new ArgumentException("Effect presentation requires a valid EffectId.", nameof(effectId));

            EffectId = effectId;
            Channel = channel;
            EffectInstanceId = effectInstanceId;
            SourceDiceId = sourceDiceId;
            TargetDiceId = targetDiceId;
        }

        internal EffectId EffectId { get; }
        internal PresentationChannel Channel { get; }
        internal EffectInstanceId EffectInstanceId { get; }
        internal DiceInstanceId SourceDiceId { get; }
        internal DiceInstanceId TargetDiceId { get; }
    }

    /// <summary>
    /// Editor-authored mapping for one EffectId + presentation channel. Multiple entries on one DiceView may be active
    /// independently, allowing effect presentations to overlap or queue without collapsing every effect into one state.
    /// The owning DiceView supplies its single shared Animator.
    /// </summary>
    [Serializable]
    public sealed class EffectPresentationBinding
    {
        [SerializeField] private EffectId _effectId;
        [SerializeField] private PresentationChannel _channel;
        [Tooltip("Animation Event key used only to report completion for this authored one-shot presentation.")]
        [SerializeField] private string _completionKey;
        [SerializeField] private AnimatorTriggerPresentationBinding _oneShotPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _persistentPresentation = new AnimatorBoolPresentationBinding();

        [NonSerialized] private Queue<Action> _pendingCompletions;

        internal EffectId EffectId => _effectId;
        internal PresentationChannel Channel => _channel;
        internal string CompletionKey => _completionKey ?? string.Empty;
        internal bool IsOneShotConfigured => _oneShotPresentation.IsConfigured;
        internal bool IsPersistentConfigured => _persistentPresentation.IsConfigured;

        internal void BindAnimator(Animator animator)
        {
            _oneShotPresentation.BindAnimator(animator);
            _persistentPresentation.BindAnimator(animator);
        }

        public bool TryValidate(string label, out string error)
        {
            if (!_effectId.IsValid)
            {
                error = $"{label}: EffectId must be a valid effect.name ID.";
                return false;
            }

            if (!_oneShotPresentation.TryValidate($"{label} One Shot", out error)
                || !_persistentPresentation.TryValidate($"{label} Persistent", out error))
            {
                return false;
            }

            if (_oneShotPresentation.IsConfigured && string.IsNullOrWhiteSpace(_completionKey))
            {
                error = $"{label}: Completion Key is required when a one-shot Animator presentation is configured.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal bool Matches(EffectId effectId, PresentationChannel channel) =>
            _effectId == effectId && _channel == channel;

        internal void Play(Action onCompleted)
        {
            if (!_oneShotPresentation.Play())
            {
                onCompleted?.Invoke();
                return;
            }

            PendingCompletions.Enqueue(onCompleted);
        }

        internal void SetPersistent(bool isActive) => _persistentPresentation.SetActive(isActive);

        internal bool TryComplete(string completionKey)
        {
            if (!string.Equals(CompletionKey, completionKey, StringComparison.Ordinal)
                || _pendingCompletions == null
                || _pendingCompletions.Count == 0)
            {
                return false;
            }

            Action callback = _pendingCompletions.Dequeue();
            callback?.Invoke();
            return true;
        }

        internal void CancelAll()
        {
            _pendingCompletions?.Clear();
            _oneShotPresentation.Cancel();
            _persistentPresentation.SetActive(false);
        }

        private Queue<Action> PendingCompletions =>
            _pendingCompletions ?? (_pendingCompletions = new Queue<Action>());
    }
}
