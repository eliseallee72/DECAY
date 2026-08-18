
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decay
{
    [Serializable]
    public sealed class DiceFaceDefinition
    {
        [SerializeField, Min(1)] private int _faceIndex = 1;
        [SerializeField, Range(BattleRules.MinimumRollValue, BattleRules.MaximumRollValue)] private int _rollValue = 1;
        [SerializeField] private int _baseScoreValue = 1;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private List<EffectDefinition> _effects = new List<EffectDefinition>();

        public int FaceIndex => _faceIndex;
        public int RollValue => _rollValue;
        public int BaseScoreValue => _baseScoreValue;
        public Sprite Sprite => _sprite;
        public IReadOnlyList<EffectDefinition> Effects => _effects;

        public DiceFaceDefinition()
        {
        }

        internal DiceFaceDefinition(
            int faceIndex,
            int rollValue,
            int baseScoreValue,
            Sprite sprite = null,
            IEnumerable<EffectDefinition> effects = null)
        {
            _faceIndex = faceIndex;
            _rollValue = rollValue;
            _baseScoreValue = baseScoreValue;
            _sprite = sprite;
            _effects = effects == null
                ? new List<EffectDefinition>()
                : new List<EffectDefinition>(effects);
        }

        public bool TryValidate(out string error)
        {
            if (_faceIndex < 1)
            {
                error = "Face index must be at least 1.";
                return false;
            }

            if (!RollValueRules.IsValid(_rollValue))
            {
                error = $"Face {_faceIndex}: roll value must be between {BattleRules.MinimumRollValue} and {BattleRules.MaximumRollValue}.";
                return false;
            }

            if (_effects == null)
            {
                error = $"Face {_faceIndex}: effects collection is missing.";
                return false;
            }

            var effectIds = new HashSet<EffectId>();
            for (int i = 0; i < _effects.Count; i++)
            {
                EffectDefinition effect = _effects[i];
                if (effect == null)
                {
                    error = $"Face {_faceIndex}: effect {i + 1} is missing.";
                    return false;
                }

                if (!effect.TryValidate(out string effectError))
                {
                    error = $"Face {_faceIndex}: effect {i + 1} is invalid: {effectError}";
                    return false;
                }

                if (!effectIds.Add(effect.Id))
                {
                    error = $"Face {_faceIndex}: effect IDs must be unique within that face.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
