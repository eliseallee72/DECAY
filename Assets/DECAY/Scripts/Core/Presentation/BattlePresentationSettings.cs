using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation tuning for DECAY visual processes. Values here never determine gameplay outcome.
    /// Empty/zero coded-motion settings mean the associated presentation layer is intentionally unconfigured.
    /// </summary>
    [Serializable]
    public sealed class BattlePresentationSettings
    {
        [Serializable]
        public sealed class CodedMotionSettings
        {
            [Tooltip("Editor-authored duration. Zero means this coded destination motion is not configured.")]
            [SerializeField, Min(0f)] private float _duration;
            [Tooltip("Editor-authored normalized easing. No fallback easing is supplied by code.")]
            [SerializeField] private AnimationCurve _easing = new AnimationCurve();
            [Tooltip("Use unscaled time for this presentation-only destination motion.")]
            [SerializeField] private bool _useUnscaledTime;

            public float Duration => _duration;
            public AnimationCurve Easing => _easing;
            public bool UseUnscaledTime => _useUnscaledTime;
            public bool IsConfigured => _duration > 0f && _easing != null && _easing.length > 0;

            internal bool TryValidate(string label, out string error)
            {
                bool hasCurve = _easing != null && _easing.length > 0;
                if (_duration <= 0f && !hasCurve)
                {
                    error = string.Empty;
                    return true;
                }

                if (_duration <= 0f)
                {
                    error = $"{label}: Duration must be greater than zero when an easing curve is configured.";
                    return false;
                }

                if (!hasCurve)
                {
                    error = $"{label}: Easing curve is required when a duration is configured.";
                    return false;
                }

                error = string.Empty;
                return true;
            }
        }

        [Header("Setup")]
        [Tooltip("Start-to-start spacing for enemy setup population presentation. Zero starts eligible setup responses together.")]
        [SerializeField, Min(0f)] private float _enemySetupStartStagger;

        [Header("Roll")]
        [Tooltip("Presentation-only randomized start offset range for each die's authored Roll animation. Zero/zero starts all dice together. This never changes authoritative roll results.")]
        [SerializeField] private Vector2 _rollStartOffsetRange;

        [Header("Enemy Reposition")]
        [Tooltip("Future presentation-only delay around the Enemy Reposition cue. Authored animation completion remains event-driven.")]
        [SerializeField, Min(0f)] private float _enemyRepositionDelay;

        [Header("Process Pacing - Later Pass")]
        [SerializeField, Min(0f)] private float _decayPairStartStagger;
        [SerializeField, Min(0f)] private float _scorePairStartStagger;
        [SerializeField, Min(0f)] private float _decayToScoreDelay;

        [Header("Coded Destination Motion")]
        [Tooltip("Board-to-board swap travel. Semantic destinations still come from BattleSceneDiceLayout anchors.")]
        [SerializeField] private CodedMotionSettings _boardSwap = new CodedMotionSettings();
        [Tooltip("Placement/drop correction into a semantic board anchor. Per-die settle juice can layer afterward.")]
        [SerializeField] private CodedMotionSettings _diceSettle = new CodedMotionSettings();
        [Tooltip("Reserved for the later Inventory pass. Inventory anchors remain semantic presentation destinations.")]
        [SerializeField] private CodedMotionSettings _inventoryReturn = new CodedMotionSettings();

        public float EnemySetupStartStagger => _enemySetupStartStagger;
        public Vector2 RollStartOffsetRange => _rollStartOffsetRange;
        public float EnemyRepositionDelay => _enemyRepositionDelay;
        public float DecayPairStartStagger => _decayPairStartStagger;
        public float ScorePairStartStagger => _scorePairStartStagger;
        public float DecayToScoreDelay => _decayToScoreDelay;
        public CodedMotionSettings BoardSwap => _boardSwap;
        public CodedMotionSettings DiceSettle => _diceSettle;
        public CodedMotionSettings InventoryReturn => _inventoryReturn;

        public bool TryValidate(out string error)
        {
            if (_rollStartOffsetRange.x < 0f || _rollStartOffsetRange.y < 0f || _rollStartOffsetRange.y < _rollStartOffsetRange.x)
            {
                error = "Roll Start Offset Range must be non-negative and ordered min <= max.";
                return false;
            }

            if (!_boardSwap.TryValidate("Board Swap", out error)
                || !_diceSettle.TryValidate("Dice Settle", out error)
                || !_inventoryReturn.TryValidate("Inventory Return", out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
