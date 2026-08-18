
using System;
using System.Collections.Generic;

namespace Decay
{
    public sealed class DiceFaceRuntimeState
    {
        private readonly List<EffectDefinition> _effects;

        internal DiceFaceRuntimeState(DiceFaceSeed seed)
        {
            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            FaceIndex = seed.FaceIndex;
            RollValue = RollValueRules.Require(seed.RollValue, nameof(seed));
            ScoreValue = seed.ScoreValue;
            _effects = new List<EffectDefinition>(seed.Effects);
        }

        public int FaceIndex { get; }
        public int RollValue { get; private set; }
        public int ScoreValue { get; private set; }
        public IReadOnlyList<EffectDefinition> Effects => _effects;

        internal void SetRollValue(int value)
        {
            RollValue = RollValueRules.Require(value, nameof(value));
        }

        internal void SetScoreValue(int value)
        {
            ScoreValue = value;
        }
    }
}
