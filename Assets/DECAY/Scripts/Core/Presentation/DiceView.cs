using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Presentation for one battle dice. Authored visuals are assigned in the editor through Animator bindings;
    /// this component requests presentation only and never owns board state, inventory state, or DECAY rules.
    /// </summary>
    public sealed class DiceView : MonoBehaviour
    {
        [Header("Base Presentation")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider _interactionCollider;

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
        [SerializeField] private AnimatorTriggerPresentationBinding _effectPresentation = new AnimatorTriggerPresentationBinding();

        [Header("Predictive Presentation")]
        [SerializeField] private AnimatorBoolPresentationBinding _targetedPresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _willDecayPresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _saveSourcePresentation = new AnimatorBoolPresentationBinding();
        [SerializeField] private AnimatorBoolPresentationBinding _effectIdlePresentation = new AnimatorBoolPresentationBinding();

        private DiceInstanceId _diceId;
        private Action _enemySetupCompletion;
        private Action _rollCompletion;
        private Action _faceRevealCompletion;
        private Action _decayCompletion;
        private Action _savedCompletion;
        private Action _saviorCompletion;
        private Action _checkedCompletion;
        private Action _scoreCompletion;
        private Action _resetCompletion;
        private Action _effectCompletion;

        public DiceInstanceId DiceId => _diceId;
        public bool IsBound => _diceId.IsValid;
        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        public bool TryValidate(out string error)
        {
            if (_spriteRenderer == null)
            {
                error = $"{name}: DiceView requires a SpriteRenderer reference.";
                return false;
            }

            if (!_enemySetupPresentation.TryValidate($"{name} Enemy Setup", out error)
                || !_rollPresentation.TryValidate($"{name} Roll", out error)
                || !_faceRevealPresentation.TryValidate($"{name} Face Reveal", out error)
                || !_decayPresentation.TryValidate($"{name} Decay", out error)
                || !_savedPresentation.TryValidate($"{name} Saved", out error)
                || !_saviorPresentation.TryValidate($"{name} Savior", out error)
                || !_checkedPresentation.TryValidate($"{name} Checked", out error)
                || !_scorePresentation.TryValidate($"{name} Score", out error)
                || !_resetPresentation.TryValidate($"{name} Reset", out error)
                || !_effectPresentation.TryValidate($"{name} Effect", out error)
                || !_targetedPresentation.TryValidate($"{name} Targeted", out error)
                || !_willDecayPresentation.TryValidate($"{name} WillDecay", out error)
                || !_saveSourcePresentation.TryValidate($"{name} Save Source", out error)
                || !_effectIdlePresentation.TryValidate($"{name} Effect Idle", out error))
            {
                return false;
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

        internal void SetPresentation(Sprite sprite, Vector3 worldPosition, bool isVisible)
        {
            RequireConfigured();
            _spriteRenderer.sprite = sprite;
            transform.position = worldPosition;
            _spriteRenderer.enabled = isVisible;
            if (_interactionCollider != null)
                _interactionCollider.enabled = isVisible;
        }

        internal void SetPreviewWorldPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        internal void PlayEnemySetupPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_enemySetupPresentation, ref _enemySetupCompletion, onCompleted);

        internal void PlayRollPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_rollPresentation, ref _rollCompletion, onCompleted);

        internal void PlayFaceRevealPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_faceRevealPresentation, ref _faceRevealCompletion, onCompleted);

        internal void PlayDecayPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_decayPresentation, ref _decayCompletion, onCompleted);

        internal void PlaySavedPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_savedPresentation, ref _savedCompletion, onCompleted);

        internal void PlaySaviorPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_saviorPresentation, ref _saviorCompletion, onCompleted);

        internal void PlayCheckedPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_checkedPresentation, ref _checkedCompletion, onCompleted);

        internal void PlayScorePresentation(Action onCompleted) =>
            StartAuthoredPresentation(_scorePresentation, ref _scoreCompletion, onCompleted);

        internal void PlayResetPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_resetPresentation, ref _resetCompletion, onCompleted);

        internal void PlayEffectPresentation(Action onCompleted) =>
            StartAuthoredPresentation(_effectPresentation, ref _effectCompletion, onCompleted);

        internal void SetPredictiveDecayPresentation(bool isTargeted, bool isWillDecay, bool willCreateSave)
        {
            _targetedPresentation.SetActive(isTargeted);
            _willDecayPresentation.SetActive(isWillDecay);
            _saveSourcePresentation.SetActive(willCreateSave);
        }

        internal void SetEffectIdlePresentation(bool isActive)
        {
            _effectIdlePresentation.SetActive(isActive);
        }

        internal void ClearPredictiveDecayPresentation()
        {
            SetPredictiveDecayPresentation(false, false, false);
        }

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
            CancelEnemySetupPresentation();
            CancelRollPresentation();
            CancelFaceRevealPresentation();
            CancelOneShot(_decayPresentation, ref _decayCompletion);
            CancelOneShot(_savedPresentation, ref _savedCompletion);
            CancelOneShot(_saviorPresentation, ref _saviorCompletion);
            CancelOneShot(_checkedPresentation, ref _checkedCompletion);
            CancelOneShot(_scorePresentation, ref _scoreCompletion);
            CancelOneShot(_resetPresentation, ref _resetCompletion);
            CancelOneShot(_effectPresentation, ref _effectCompletion);
            ClearPredictiveDecayPresentation();
            SetEffectIdlePresentation(false);
        }

        // Animation Event endpoints. Authored clips invoke these at their actual completion frame.
        public void NotifyEnemySetupPresentationComplete() => CompleteOneShot(ref _enemySetupCompletion);
        public void NotifyRollPresentationComplete() => CompleteOneShot(ref _rollCompletion);
        public void NotifyFaceRevealPresentationComplete() => CompleteOneShot(ref _faceRevealCompletion);
        public void NotifyDecayPresentationComplete() => CompleteOneShot(ref _decayCompletion);
        public void NotifySavedPresentationComplete() => CompleteOneShot(ref _savedCompletion);
        public void NotifySaviorPresentationComplete() => CompleteOneShot(ref _saviorCompletion);
        public void NotifyCheckedPresentationComplete() => CompleteOneShot(ref _checkedCompletion);
        public void NotifyScorePresentationComplete() => CompleteOneShot(ref _scoreCompletion);
        public void NotifyResetPresentationComplete() => CompleteOneShot(ref _resetCompletion);
        public void NotifyEffectPresentationComplete() => CompleteOneShot(ref _effectCompletion);

        internal void ConfigureForTests(SpriteRenderer spriteRenderer, Collider interactionCollider)
        {
            _spriteRenderer = spriteRenderer;
            _interactionCollider = interactionCollider;
        }

        private void Awake()
        {
            RequireConfigured();
        }

        private void OnDisable()
        {
            CancelAllPresentation();
        }

        private void OnValidate()
        {
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_interactionCollider == null)
                _interactionCollider = GetComponent<Collider>();
        }

        private static void StartAuthoredPresentation(
            AnimatorTriggerPresentationBinding binding,
            ref Action pendingCompletion,
            Action onCompleted)
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
