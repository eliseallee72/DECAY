using System;

namespace Decay
{
    /// <summary>
    /// Seeded gameplay randomness for normal battles. A battle composition root should create
    /// and share the appropriate source rather than individual dice calling random APIs directly.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SeededRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInt(int minimumInclusive, int maximumExclusive)
        {
            RequireValidRange(minimumInclusive, maximumExclusive);
            return _random.Next(minimumInclusive, maximumExclusive);
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
