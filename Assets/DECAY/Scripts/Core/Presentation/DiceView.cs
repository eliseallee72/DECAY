using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation for one battle dice. Gameplay state owns identity/location/availability; this View receives
    /// authoritative visual content and semantic destinations. Authored Animator responses and optional editor-authored
    /// procedural transform layers may be combined without either becoming gameplay authority.
    /// </summary>
    public sealed class DiceView : MonoBehaviour, IPointerPresentationTarget
    {
        [Header("Base Presentation")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider _interactionCollider;
        [Tooltip("Optional editor-authored trigger used to return transient animation to the persistent authoritative visual state after reconciliation.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _reconcilePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Pointer Presentation")]
        [Tooltip("Presentation-only hover state. Gameplay availability is still decided by authoritative interaction gates.")]
        [SerializeField] private AnimatorBoolPresentationBinding _hoverPresentation = new AnimatorBoolPresentationBinding();
        [Tooltip("Optional decorative press response. It does not approve movement, selection, or any other gameplay request.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _decorativePressPresentation = new AnimatorTriggerPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _decorativePressMotion = new ProceduralTransformPresentationBinding();

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
        [Tooltip("Optional authored response used after a destination movement such as a board swap or enemy population.")]
        [SerializeField] private AnimatorTriggerPresentationBinding _settlePresentation = new AnimatorTriggerPresentationBinding();

        [Header("Optional Procedural Layers")]
        [Tooltip("Each layer is entirely Inspector-authored. Leave it empty for Animator-only presentation.")]
        [SerializeField] private ProceduralTransformPresentationBinding _enemySetupMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _rollMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _faceRevealMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _decayMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _savedMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _saviorMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _checkedMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _scoreMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _resetMotion = new ProceduralTransformPresentationBinding();
        [SerializeField] private ProceduralTransformPresentationBinding _settleMotion = new ProceduralTransformPresentationBinding();

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
        private HybridOneShotPresentationRun _decorativePressRun;
        private HybridOneShotPresentationRun _enemySetupRun;
        private HybridOneShotPresentationRun _rollRun;
        private HybridOneShotPresentationRun _faceRevealRun;
        private HybridOneShotPresentationRun _decayRun;
        private HybridOneShotPresentationRun _savedRun;
        private HybridOneShotPresentationRun _saviorRun;
        private HybridOneShotPresentationRun _checkedRun;
        private HybridOneShotPresentationRun _scoreRun;
        private HybridOneShotPresentationRun _resetRun;
        private HybridOneShotPresentationRun _settleRun;

        public DiceInstanceId DiceId => _diceId;
        public bool IsBound => _diceId.IsValid;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public bool HasPresentationDestination => _hasPresentationDestination;
        public Vector3 PresentationDestinationWorldPosition => _hasPresentationDestination ? _presentationDestination.WorldPosition : transform.position;
        bool IPointerPresentationTarget.PointerPresentationEnabled =>
            isActiveAndEnabled && _interactionCollider != null && _interactionCollider.enabled;

        public bool TryValidate(out string error)
        {
            if (_spriteRenderer == null)
            {
                error = $"{name}: DiceView requires a SpriteRenderer reference.";
                return false;
            }

            if (!_reconcilePresentation.TryValidate($"{name} Reconcile", out error)
                || !_hoverPresentation.TryValidate($"{name} Hover", out error)
                || !_decorativePressPresentation.TryValidate($"{name} Decorative Press", out error)
                || !_decorativePressMotion.TryValidate($"{name} Decorative Press Motion", out error)
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
                || !_enemySetupMotion.TryValidate($"{name} Enemy Setup Motion", out error)
                || !_rollMotion.TryValidate($"{name} Roll Motion", out error)
                || !_faceRevealMotion.TryValidate($"{name} Face Reveal Motion", out error)
                || !_decayMotion.TryValidate($"{name} Decay Motion", out error)
                || !_savedMotion.TryValidate($"{name} Saved Motion", out error)
                || !_saviorMotion.TryValidate($"{name} Savior Motion", out error)
                || !_checkedMotion.TryValidate($"{name} Checked Motion", out error)
                || !_scoreMotion.TryValidate($"{name} Score Motion", out error)
                || !_resetMotion.TryValidate($"{name} Reset Motion", out error)
                || !_settleMotion.TryValidate($"{name} Settle Motion", out error)
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

        internal void PlayEnemySetupPresentation(Action onCompleted) =>
            StartHybrid(_enemySetupPresentation, _enemySetupMotion, ref _enemySetupRun, onCompleted);
        internal void PlayRollPresentation(Action onCompleted) =>
            StartHybrid(_rollPresentation, _rollMotion, ref _rollRun, onCompleted);
        internal void PlayFaceRevealPresentation(Action onCompleted) =>
            StartHybrid(_faceRevealPresentation, _faceRevealMotion, ref _faceRevealRun, onCompleted);
        internal void PlayDecayPresentation(Action onCompleted) =>
            StartHybrid(_decayPresentation, _decayMotion, ref _decayRun, onCompleted);
        internal void PlaySavedPresentation(Action onCompleted) =>
            StartHybrid(_savedPresentation, _savedMotion, ref _savedRun, onCompleted);
        internal void PlaySaviorPresentation(Action onCompleted) =>
            StartHybrid(_saviorPresentation, _saviorMotion, ref _saviorRun, onCompleted);
        internal void PlayCheckedPresentation(Action onCompleted) =>
            StartHybrid(_checkedPresentation, _checkedMotion, ref _checkedRun, onCompleted);
        internal void PlayScorePresentation(Action onCompleted) =>
            StartHybrid(_scorePresentation, _scoreMotion, ref _scoreRun, onCompleted);
        internal void PlayResetPresentation(Action onCompleted) =>
            StartHybrid(_resetPresentation, _resetMotion, ref _resetRun, onCompleted);
        internal void PlaySettlePresentation(Action onCompleted) =>
            StartHybrid(_settlePresentation, _settleMotion, ref _settleRun, onCompleted);

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

        internal void CancelEnemySetupPresentation() => CancelRun(ref _enemySetupRun);
        internal void CancelRollPresentation() => CancelRun(ref _rollRun);
        internal void CancelFaceRevealPresentation() => CancelRun(ref _faceRevealRun);

        internal void CancelAllPresentation()
        {
            CancelRun(ref _decorativePressRun);
            CancelRun(ref _enemySetupRun);
            CancelRun(ref _rollRun);
            CancelRun(ref _faceRevealRun);
            CancelRun(ref _decayRun);
            CancelRun(ref _savedRun);
            CancelRun(ref _saviorRun);
            CancelRun(ref _checkedRun);
            CancelRun(ref _scoreRun);
            CancelRun(ref _resetRun);
            CancelRun(ref _settleRun);
            for (int i = 0; i < _effectPresentations.Count; i++)
                _effectPresentations[i]?.CancelAll();
            _hoverPresentation.SetActive(false);
            ClearPredictiveDecayPresentation();
        }

        public void NotifyDecorativePressPresentationComplete() => CompleteAuthored(ref _decorativePressRun);
        public void NotifyEnemySetupPresentationComplete() => CompleteAuthored(ref _enemySetupRun);
        public void NotifyRollPresentationComplete() => CompleteAuthored(ref _rollRun);
        public void NotifyFaceRevealPresentationComplete() => CompleteAuthored(ref _faceRevealRun);
        public void NotifyDecayPresentationComplete() => CompleteAuthored(ref _decayRun);
        public void NotifySavedPresentationComplete() => CompleteAuthored(ref _savedRun);
        public void NotifySaviorPresentationComplete() => CompleteAuthored(ref _saviorRun);
        public void NotifyCheckedPresentationComplete() => CompleteAuthored(ref _checkedRun);
        public void NotifyScorePresentationComplete() => CompleteAuthored(ref _scoreRun);
        public void NotifyResetPresentationComplete() => CompleteAuthored(ref _resetRun);
        public void NotifySettlePresentationComplete() => CompleteAuthored(ref _settleRun);

        /// <summary>Animation Event endpoint. The key is editor-authored on the matching effect presentation binding.</summary>
        public void NotifyEffectPresentationComplete(string completionKey)
        {
            for (int i = 0; i < _effectPresentations.Count; i++)
            {
                if (_effectPresentations[i] != null && _effectPresentations[i].TryComplete(completionKey))
                    return;
            }
        }

        internal void ConfigureForTests(SpriteRenderer spriteRenderer, Collider interactionCollider)
        {
            _spriteRenderer = spriteRenderer;
            _interactionCollider = interactionCollider;
        }

        void IPointerPresentationTarget.SetPointerHovered(bool isHovered) =>
            _hoverPresentation.SetActive(isHovered);

        void IPointerPresentationTarget.PlayPointerPressPresentation() =>
            StartHybrid(_decorativePressPresentation, _decorativePressMotion, ref _decorativePressRun, null);

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

        private void Awake() => RequireConfigured();
        private void OnDisable() => CancelAllPresentation();

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
        }

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

        private static void CompleteAuthored(ref HybridOneShotPresentationRun run)
        {
            run?.NotifyAuthoredComplete();
        }

        private void RequireConfigured()
        {
            if (_spriteRenderer == null)
                throw new InvalidOperationException($"{name}: DiceView requires a SpriteRenderer reference.");
        }
    }
}
