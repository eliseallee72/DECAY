using System;

namespace Decay
{
    /// <summary>
    /// Resolves which authored/runtime face a dice lands on. The selected face index is authoritative;
    /// Roll Value is read from that face after selection and may differ from the face index.
    /// </summary>
    public sealed class DiceRollResolver
    {
        private readonly IRandomSource _randomSource;

        public DiceRollResolver(IRandomSource randomSource)
        {
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public int ResolveFaceIndex(DiceRuntimeState diceState)
        {
            if (diceState == null)
            {
                throw new ArgumentNullException(nameof(diceState));
            }

            if (diceState.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException($"DECAYED dice {diceState.InstanceId} cannot be rolled this game.");
            }

            if (diceState.Faces.Count == 0)
            {
                throw new InvalidOperationException($"Dice {diceState.InstanceId} has no faces to roll.");
            }

            // Runtime face indices are explicit and 1-based. Keeping the random draw in the same
            // domain means scripted tutorial values can name face 1, face 2, etc. directly.
            int selectedFaceIndex = _randomSource.NextInt(1, diceState.Faces.Count + 1);
            if (!diceState.TryGetFace(selectedFaceIndex, out _))
            {
                throw new InvalidOperationException(
                    $"Random source selected face {selectedFaceIndex}, which is not present on dice {diceState.InstanceId}.");
            }

            return selectedFaceIndex;
        }
    }
}
