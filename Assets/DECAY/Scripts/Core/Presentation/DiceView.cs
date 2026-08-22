using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation for one battle dice. Gameplay state owns identity/location/availability; this View receives
    /// authoritative visual content and semantic destinations, while one editor-assigned Animator Controller and its
    /// Animation Clips own authored visual timing, transforms, alpha, layering/blending, sprite changes, and curves.
    /// Runtime destination movement remains a separate presentation concern because its endpoint is authoritative data.
    /// </summary>
    public sealed class DiceView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Base Presentation")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider _interactionCollider;
        [Tooltip("Single Animator used by this DiceView's authored animation parameters. If left empty, DiceView auto-finds an Animator on this object or its children. Assign your 2D Animator Controller on that Animator component.")]
        [SerializeField] private Animator _animator;
        [Tooltip("Optional editor-authored trigger used to return transient animation to the persistent authoritative visual state after reconciliation.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Pointer Presentation")]
        [Tooltip("Presentation-only hover state. Gameplay availability is still decided by authoritative interaction gates.")]
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();
        [Tooltip("Optional authored decorative press response. It does not approve movement, selection, or any other gameplay request.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();

        [Header("Authored One-Shot Presentation")]
        [SerializeField] private AnimatorTriggerPresentationBinding _enemySetupPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _rollPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _faceRevealPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _decayPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _savedPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _saviorPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _checkedPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _scorePresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private AnimatorTriggerPresentationBinding _resetPresentation = new AnimatorTriggerPresentationBinding();
        [Tooltip("Optional authored response used after runtime destination movement such as a board swap or enemy population.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _settlePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Predictive Presentation")]
        [SerializeField] private AnimatorBoolPresentationBinding _targetedPresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _willDecayPresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _saveSourcePresentation = new AnimatorBoolPresentationBinding();

        [Header("Effect Presentation Registry")]
        [Tooltip("Editor-authored EffectId/channel mappings. Multiple entries may run independently; gameplay effect rules are never interpreted here.")]
        [SerializeField] private List<EffectPresentationBinding> _effectPresentations = new List<EffectPresentationBinding>();

        private DiceInstanceId _diceId;
        private DicePresentationDestination _presentationDestination;
        private bool _hasPresentationDestination;
        private Action _enemySetupCompletion;
        private Action _rollCompletion;
        private Action _faceRevealCompletion;
        private Action _decayCompletion;
        private Action _savedCompletion;
        private Action _saviorCompletion;
        private Action _checkedCompletion;
        private Action _scoreCompletion;
        private Action _resetCompletion;
        private Action _settleCompletion;

        public DiceInstanceId DiceId => _diceId;
        public bool IsBound => _diceId.IsValid;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public bool HasPresentationDestination => _hasPresentationDestination;
        public Vector3 PresentationDestinationWorldPosition => _hasPresentationDestination ? _presentationDestination.WorldPosition : transform.position;
        bool IPointerPresentationTarget.PointerPresentationEnabled =>
            isActiveAndEnabled && _interactionCollider != null && _interactionCollider.enabled;

        public bool TryValidate(out string error)
        {
            BindPresentationAnimator();

            if (_spriteRenderer == null)
            {
                error = $"{name}: DiceView requires a SpriteRenderer reference.";
                return false;
            }

            if (!_reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                || !_hoverPresentation.TryValidate($"{name} Hover", out error)
                || !_decorativePressPresentation.TryValidate($"{name} Decorative Press", out error)
                || !_enemySetupPresentation.TryValidate($"{name} Enemy Setup", out error)
                || !_rollPresentation.TryValidate($"{name} Roll", out error)
                || !_faceRevealPresentation.TryValidate($"{name} Face Reveal", out error)
                || !_decayPresentation.TryValidate($"{name} Decay", out error)
                || !_savedPresentation.TryValidate($"{name} Saved", out error)
                || !_saviorPresentation.TryValidate($"{name} Savior", out error)
                || !_checkedPresentation.TryValidate($"{name} Checked", out error)
                || !_scorePresentation.TryValidate($"{name} Score", out error)
                || !_resetPresentation.TryValidate($"{name} Reset", out error)
                || !_settlePresentation.TryValidate($"{name} Settle", out error)
                || !_targetedPresentation.TryValidate($"{name} Targeted", out error)
                || !_willDecayPresentation.TryValidate($"{name} WillDecay", out error)
                || !_saveSourcePresentation.TryValidate($"{name} Save Source", out error))
            {
                return false;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            var mappings = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _effectPresentations.Count; i++)
            {
                EffectPresentationBinding binding = _effectPresentations[i];
                if (binding == null)
                {
                    error = $"{name}: Effect Presentation entry {i + 1} is null.";
                    return false;
                }
                if (!binding.TryValidate($"{name} Effect Presentation {i + 1}", out error))
                    return false;

                string mappingKey = $"{binding.EffectId.Value}|{(int)binding.Channel}";
                if (!mappings.Add(mappingKey))
                {
                    error = $"{name}: Effect {binding.EffectId} has more than one mapping for channel {binding.Channel}.";
                    return false;
                }

                if (binding.IsOneShotConfigured && !keys.Add(binding.CompletionKey))
                {
                    error = $"{name}: Effect presentation completion key '{binding.CompletionKey}' is duplicated.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public void Bind(DiceInstanceId diceId)
        {
            if (!diceId.IsValid)
                throw new ArgumentException("DiceView requires a valid DiceInstanceId.", nameof(diceId));
            if (IsBound && _diceId != diceId)
                throw new InvalidOperationException($"DiceView is already bound to dice {_diceId}.");
            _diceId = diceId;
        }

        internal void SetVisualContent(Sprite sprite, bool isVisible)
        {
            RequireConfigured();
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = isVisible;
        }

        internal void SetInteractionSurfaceEnabled(bool isEnabled)
        {
            if (_interactionCollider != null)
                _interactionCollider.enabled = isEnabled;
            if (!isEnabled)
                _hoverPresentation.SetActive(false);
        }

        internal void SetPresentationDestination(DicePresentationDestination destination)
        {
            _presentationDestination = destination;
            _hasPresentationDestination = true;
        }

        internal void ReconcileRenderedTransformToDestination()
        {
            if (_hasPresentationDestination)
                transform.position = _presentationDestination.WorldPosition;
        }

        internal void ReconcileAuthoritativePresentation(bool invokeRecoveryHook)
        {
            ReconcileRenderedTransformToDestination();
            if (invokeRecoveryHook)
                _reconcilePresentation.Play();
        }

        internal void SetPreviewWorldPosition(Vector3 worldPosition) => transform.position = worldPosition;

        internal void PlayEnemySetupPresentation(Action onCompleted) => StartAuthoredPresentation(_enemySetupPresentation, ref _enemySetupCompletion, onCompleted);
        internal void PlayRollPresentation(Action onCompleted) => StartAuthoredPresentation(_rollPresentation, ref _rollCompletion, onCompleted);
        internal void PlayFaceRevealPresentation(Action onCompleted) => StartAuthoredPresentation(_faceRevealPresentation, ref _faceRevealCompletion, onCompleted);
        internal void PlayDecayPresentation(Action onCompleted) => StartAuthoredPresentation(_decayPresentation, ref _decayCompletion, onCompleted);
        internal void PlaySavedPresentation(Action onCompleted) => StartAuthoredPresentation(_savedPresentation, ref _savedCompletion, onCompleted);
        internal void PlaySaviorPresentation(Action onCompleted) => StartAuthoredPresentation(_saviorPresentation, ref _saviorCompletion, onCompleted);
        internal void PlayCheckedPresentation(Action onCompleted) => StartAuthoredPresentation(_checkedPresentation, ref _checkedCompletion, onCompleted);
        internal void PlayScorePresentation(Action onCompleted) => StartAuthoredPresentation(_scorePresentation, ref _scoreCompletion, onCompleted);
        internal void PlayResetPresentation(Action onCompleted) => StartAuthoredPresentation(_resetPresentation, ref _resetCompletion, onCompleted);
        internal void PlaySettlePresentation(Action onCompleted) => StartAuthoredPresentation(_settlePresentation, ref _settleCompletion, onCompleted);

        internal void PlayEffectPresentation(EffectPresentationRequest request, Action onCompleted)
        {
            EffectPresentationBinding binding = FindEffectBinding(request.EffectId, request.Channel);
            if (binding == null)
            {
                onCompleted?.Invoke();
                return;
            }
            binding.Play(onCompleted);
        }

        internal void SetEffectPersistentPresentation(EffectId effectId, PresentationChannel channel, bool isActive)
        {
            FindEffectBinding(effectId, channel)?.SetPersistent(isActive);
        }

        internal void SetPredictiveDecayPresentation(bool isTargeted, bool isWillDecay, bool willCreateSave)
        {
            _targetedPresentation.SetActive(isTargeted);
            _willDecayPresentation.SetActive(isWillDecay);
            _saveSourcePresentation.SetActive(willCreateSave);
        }

        internal void ClearPredictiveDecayPresentation() => SetPredictiveDecayPresentation(false, false, false);

        internal void CancelEnemySetupPresentation()
        {
            _enemySetupCompletion = null;
            _enemySetupPresentation.Cancel();
        }

        internal void CancelRollPresentation()
        {
            _rollCompletion = null;
            _rollPresentation.Cancel();
        }

        internal void CancelFaceRevealPresentation()
        {
            _faceRevealCompletion = null;
            _faceRevealPresentation.Cancel();
        }

        internal void CancelAllPresentation()
        {
            _decorativePressPresentation.Cancel();
            CancelEnemySetupPresentation();
            CancelRollPresentation();
            CancelFaceRevealPresentation();
            CancelOneShot(_decayPresentation, ref _decayCompletion);
            CancelOneShot(_savedPresentation, ref _savedCompletion);
            CancelOneShot(_saviorPresentation, ref _saviorCompletion);
            CancelOneShot(_checkedPresentation, ref _checkedCompletion);
            CancelOneShot(_scorePresentation, ref _scoreCompletion);
            CancelOneShot(_resetPresentation, ref _resetCompletion);
            CancelOneShot(_settlePresentation, ref _settleCompletion);
            for (int i = 0; i < _effectPresentations.Count; i++)
                _effectPresentations[i]?.CancelAll();
            _hoverPresentation.SetActive(false);
            ClearPredictiveDecayPresentation();
        }

        public void NotifyEnemySetupPresentationComplete() => CompleteOneShot(ref _enemySetupCompletion);
        public void NotifyRollPresentationComplete() => CompleteOneShot(ref _rollCompletion);
        public void NotifyFaceRevealPresentationComplete() => CompleteOneShot(ref _faceRevealCompletion);
        public void NotifyDecayPresentationComplete() => CompleteOneShot(ref _decayCompletion);
        public void NotifySavedPresentationComplete() => CompleteOneShot(ref _savedCompletion);
        public void NotifySaviorPresentationComplete() => CompleteOneShot(ref _saviorCompletion);
        public void NotifyCheckedPresentationComplete() => CompleteOneShot(ref _checkedCompletion);
        public void NotifyScorePresentationComplete() => CompleteOneShot(ref _scoreCompletion);
        public void NotifyResetPresentationComplete() => CompleteOneShot(ref _resetCompletion);
        public void NotifySettlePresentationComplete() => CompleteOneShot(ref _settleCompletion);

        /// <summary>Animation Event endpoint. The key is editor-authored on the matching effect presentation binding.</summary>
        public void NotifyEffectPresentationComplete(string completionKey)
        {
            for (int i = 0; i < _effectPresentations.Count; i++)
            {
                if (_effectPresentations[i] != null && _effectPresentations[i].TryComplete(completionKey))
                    return;
            }
        }

        internal void ConfigureForTests(SpriteRenderer spriteRenderer, Collider interactionCollider, Animator animator = null)
        {
            _spriteRenderer = spriteRenderer;
            _interactionCollider = interactionCollider;
            if (animator != null)
                _animator = animator;
            BindPresentationAnimator();
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) => _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation() => _decorativePressPresentation.Play();

        private EffectPresentationBinding FindEffectBinding(EffectId effectId, PresentationChannel channel)
        {
            for (int i = 0; i < _effectPresentations.Count; i++)
            {
                EffectPresentationBinding binding = _effectPresentations[i];
                if (binding != null && binding.Matches(effectId, channel))
                    return binding;
            }
            return null;
        }

        private void Awake()
        {
            ResolveAnimatorReference();
            BindPresentationAnimator();
            RequireConfigured();
        }

        private void OnDisable() => CancelAllPresentation();

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
            ResolveAnimatorReference();
            BindPresentationAnimator();
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
            _reconcilePresentation.BindAnimator(_animator);
            _hoverPresentation.BindAnimator(_animator);
            _decorativePressPresentation.BindAnimator(_animator);
            _enemySetupPresentation.BindAnimator(_animator);
            _rollPresentation.BindAnimator(_animator);
            _faceRevealPresentation.BindAnimator(_animator);
            _decayPresentation.BindAnimator(_animator);
            _savedPresentation.BindAnimator(_animator);
            _saviorPresentation.BindAnimator(_animator);
            _checkedPresentation.BindAnimator(_animator);
            _scorePresentation.BindAnimator(_animator);
            _resetPresentation.BindAnimator(_animator);
            _settlePresentation.BindAnimator(_animator);
            _targetedPresentation.BindAnimator(_animator);
            _willDecayPresentation.BindAnimator(_animator);
            _saveSourcePresentation.BindAnimator(_animator);

            for (int i = 0; i < _effectPresentations.Count; i++)
                _effectPresentations[i]?.BindAnimator(_animator);
        }

        private static void StartAuthoredPresentation(AnimatorTriggerPresentationBinding binding, ref Action pendingCompletion, Action onCompleted)
        {
            pendingCompletion = null;
            if (!binding.Play())
            {
                onCompleted?.Invoke();
                return;
            }
            pendingCompletion = onCompleted;
        }

        private static void CancelOneShot(AnimatorTriggerPresentationBinding binding, ref Action pendingCompletion)
        {
            pendingCompletion = null;
            binding.Cancel();
        }

        private static void CompleteOneShot(ref Action pendingCompletion)
        {
            Action callback = pendingCompletion;
            pendingCompletion = null;
            callback?.Invoke();
        }

        private void RequireConfigured()
        {
            if (_spriteRenderer == null)
                throw new InvalidOperationException($"{name}: DiceView requires a SpriteRenderer reference.");
        }
    }
}
