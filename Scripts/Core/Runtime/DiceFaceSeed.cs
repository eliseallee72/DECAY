
using System;
using System.Collections.Generic;

namespace Decay
{
    public sealed class DiceFaceSeed
    {
        private readonly List<EffectDefinition> _effects;

        internal DiceFaceSeed(
            int faceIndex,
            int rollValue,
            int scoreValue,
            IEnumerable<EffectDefinition> effects = null)
        {
            if (faceIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(faceIndex), faceIndex, "Face index must be at least 1.");
            }

            FaceIndex = faceIndex;
            RollValue = RollValueRules.Require(rollValue, nameof(rollValue));
            ScoreValue = scoreValue;
            _effects = effects == null
                ? new List<EffectDefinition>()
                : new List<EffectDefinition>(effects);
        }

        public int FaceIndex { get; }
        public int RollValue { get; }
        public int ScoreValue { get; }
        public IReadOnlyList<EffectDefinition> Effects => _effects;
    }
}
