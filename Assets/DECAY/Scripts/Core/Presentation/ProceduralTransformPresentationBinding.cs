using System;
using System.Collections;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Optional editor-authored procedural transform layer for a presentation response.
    /// Code only evaluates serialized curves and offsets; it does not provide visual defaults.
    /// Use a child/visual transform when an Animator also drives transform properties so authored
    /// animation and procedural motion can be layered without competing for the same properties.
    /// </summary>
    [Serializable]
    public sealed class ProceduralTransformPresentationBinding
    {
        [Tooltip("Transform driven by this procedural presentation layer. Prefer a dedicated visual child when an Animator also animates transforms.")]
        [SerializeField] private Transform _target;
        [Tooltip("Editor-authored duration. Zero leaves this procedural layer unconfigured.")]
        [SerializeField, Min(0f)] private float _duration;
        [Tooltip("Use unscaled time for this presentation layer. Timing remains presentation-only.")]
        [SerializeField] private bool _useUnscaledTime;

        [Header("Local Position")]
        [SerializeField] private Vector3 _localPositionOffset;
        [Tooltip("Normalized time -> amount of Local Position Offset. Empty means this channel is not driven.")]
        [SerializeField] private AnimationCurve _localPositionCurve = new AnimationCurve();

        [Header("Local Rotation")]
        [SerializeField] private Vector3 _localEulerOffset;
        [Tooltip("Normalized time -> amount of Local Euler Offset. Empty means this channel is not driven.")]
        [SerializeField] private AnimationCurve _localRotationCurve = new AnimationCurve();

        [Header("Local Scale")]
        [SerializeField] private Vector3 _localScaleOffset;
        [Tooltip("Normalized time -> amount of Local Scale Offset. Empty means this channel is not driven.")]
        [SerializeField] private AnimationCurve _localScaleCurve = new AnimationCurve();

        [NonSerialized] private MonoBehaviour _owner;
        [NonSerialized] private Coroutine _routine;
        [NonSerialized] private bool _hasCapturedBase;
        [NonSerialized] private Vector3 _baseLocalPosition;
        [NonSerialized] private Quaternion _baseLocalRotation;
        [NonSerialized] private Vector3 _baseLocalScale;

        public bool IsConfigured =>
            _target != null
            && _duration > 0f
            && (DrivesPosition || DrivesRotation || DrivesScale);

        private bool DrivesPosition => _localPositionOffset != Vector3.zero && HasCurve(_localPositionCurve);
        private bool DrivesRotation => _localEulerOffset != Vector3.zero && HasCurve(_localRotationCurve);
        private bool DrivesScale => _localScaleOffset != Vector3.zero && HasCurve(_localScaleCurve);

        public bool TryValidate(string label, out string error)
        {
            bool hasAnyAuthoredValue = _target != null
                || _duration > 0f
                || _localPositionOffset != Vector3.zero
                || _localEulerOffset != Vector3.zero
                || _localScaleOffset != Vector3.zero
                || HasCurve(_localPositionCurve)
                || HasCurve(_localRotationCurve)
                || HasCurve(_localScaleCurve);

            if (!hasAnyAuthoredValue)
            {
                error = string.Empty;
                return true;
            }

            if (_target == null)
            {
                error = $"{label}: Target is required when procedural motion is configured.";
                return false;
            }

            if (_duration <= 0f)
            {
                error = $"{label}: Duration must be greater than zero when procedural motion is configured.";
                return false;
            }

            if (_localPositionOffset != Vector3.zero && !HasCurve(_localPositionCurve))
            {
                error = $"{label}: Local Position Curve is required when Local Position Offset is non-zero.";
                return false;
            }

            if (_localEulerOffset != Vector3.zero && !HasCurve(_localRotationCurve))
            {
                error = $"{label}: Local Rotation Curve is required when Local Euler Offset is non-zero.";
                return false;
            }

            if (_localScaleOffset != Vector3.zero && !HasCurve(_localScaleCurve))
            {
                error = $"{label}: Local Scale Curve is required when Local Scale Offset is non-zero.";
                return false;
            }

            if (!DrivesPosition && !DrivesRotation && !DrivesScale)
            {
                error = $"{label}: Configure at least one non-zero transform offset with a curve, or clear the procedural fields.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        internal bool Play(MonoBehaviour owner, Action onCompleted)
        {
            if (!IsConfigured || owner == null)
                return false;

            Cancel();

            _owner = owner;
            _baseLocalPosition = _target.localPosition;
            _baseLocalRotation = _target.localRotation;
            _baseLocalScale = _target.localScale;
            _hasCapturedBase = true;
            _routine = owner.StartCoroutine(PlayRoutine(onCompleted));
            return true;
        }

        internal void Cancel()
        {
            if (_owner != null && _routine != null)
                _owner.StopCoroutine(_routine);

            RestoreBase();
            _routine = null;
            _owner = null;
        }

        private IEnumerator PlayRoutine(Action onCompleted)
        {
            float elapsed = 0f;
            while (elapsed < _duration)
            {
                float normalizedTime = Mathf.Clamp01(elapsed / _duration);
                Apply(normalizedTime);
                elapsed += _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }

            Apply(1f);
            RestoreBase();
            _routine = null;
            _owner = null;
            onCompleted?.Invoke();
        }

        private void Apply(float normalizedTime)
        {
            if (_target == null || !_hasCapturedBase)
                return;

            _target.localPosition = DrivesPosition
                ? _baseLocalPosition + (_localPositionOffset * _localPositionCurve.Evaluate(normalizedTime))
                : _baseLocalPosition;

            _target.localRotation = DrivesRotation
                ? _baseLocalRotation * Quaternion.Euler(_localEulerOffset * _localRotationCurve.Evaluate(normalizedTime))
                : _baseLocalRotation;

            _target.localScale = DrivesScale
                ? _baseLocalScale + (_localScaleOffset * _localScaleCurve.Evaluate(normalizedTime))
                : _baseLocalScale;
        }

        private void RestoreBase()
        {
            if (_target != null && _hasCapturedBase)
            {
                _target.localPosition = _baseLocalPosition;
                _target.localRotation = _baseLocalRotation;
                _target.localScale = _baseLocalScale;
            }

            _hasCapturedBase = false;
        }

        private static bool HasCurve(AnimationCurve curve) => curve != null && curve.length > 0;
    }
}
