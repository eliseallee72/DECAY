using System;
using UnityEngine;

namespace Decay
{
    /// <summary>
    /// Editor-authored presentation tuning reserved for known DECAY visual processes.
    /// Pass 1 exposes these values without using them to guess authored-animation completion.
    /// </summary>
    [Serializable]
    public sealed class BattlePresentationSettings
    {
        [Serializable]
        public sealed class CodedMotionSettings
        {
            [Tooltip("Editor-authored duration for a future procedural motion. Zero means not tuned yet.")]
            [SerializeField, Min(0f)] private float _duration;
            [Tooltip("Editor-authored easing for a future procedural motion. An empty curve means not tuned yet.")]
            [SerializeField] private AnimationCurve _easing = new AnimationCurve();

            public float Duration => _duration;
            public AnimationCurve Easing => _easing;
        }

        [Header("Setup")]
        [Tooltip("Future start-to-start spacing for enemy setup population. Pass 1 does not apply timing yet.")]
        [SerializeField, Min(0f)] private float _enemySetupStartStagger;

        [Header("Roll")]
        [Tooltip("Future randomized start offset range for dice Roll presentation. Pass 1 starts authored Roll hooks together.")]
        [SerializeField] private Vector2 _rollStartOffsetRange;

        [Header("Enemy Reposition")]
        [Tooltip("Future presentation-only delay around the Enemy Reposition cue. Authored animation completion remains event-driven.")]
        [SerializeField, Min(0f)] private float _enemyRepositionDelay;

        [Header("Process Pacing - Later Pass")]
        [SerializeField, Min(0f)] private float _decayPairStartStagger;
        [SerializeField, Min(0f)] private float _scorePairStartStagger;
        [SerializeField, Min(0f)] private float _decayToScoreDelay;

        [Header("Coded Motion - Later Pass")]
        [SerializeField] private CodedMotionSettings _boardSwap = new CodedMotionSettings();
        [SerializeField] private CodedMotionSettings _diceSettle = new CodedMotionSettings();
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

            error = string.Empty;
            return true;
        }
    }
}
