using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Deterministic authored randomness for tutorials and rule tests. Scripted values are checked
    /// against every requested range; invalid or exhausted scripts report a recoverable source failure.
    /// The source never chooses its own fallback; the owning gameplay process decides recovery policy.
    /// </summary>
    public sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> _scriptedValues;

        public ScriptedRandomSource(IEnumerable<int> scriptedValues)
        {
            if (scriptedValues == null)
            {
                throw new ArgumentNullException(nameof(scriptedValues));
            }

            _scriptedValues = new Queue<int>(scriptedValues);
        }

        public int RemainingCount => _scriptedValues.Count;

        public int NextInt(int minimumInclusive, int maximumExclusive)
        {
            RequireValidRange(minimumInclusive, maximumExclusive);

            if (_scriptedValues.Count == 0)
            {
                throw new RecoverableRandomSourceException(
                    "The scripted random sequence is exhausted.");
            }

            int value = _scriptedValues.Peek();
            if (value < minimumInclusive || value >= maximumExclusive)
            {
                throw new RecoverableRandomSourceException(
                    $"Scripted random value {value} is outside the requested range "
                    + $"[{minimumInclusive}, {maximumExclusive}).");
            }

            _scriptedValues.Dequeue();
            return value;
        }

        private static void RequireValidRange(int minimumInclusive, int maximumExclusive)
        {
            if (maximumExclusive <= minimumInclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumExclusive),
                    maximumExclusive,
                    "Maximum exclusive value must be greater than the minimum inclusive value.");
            }
        }
    }
}
