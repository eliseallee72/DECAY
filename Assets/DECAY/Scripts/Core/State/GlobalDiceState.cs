using System;

namespace Decay
{
    // In-memory permanent dice state. Persistence remains a separate SaveData concern.
    public sealed class GlobalDiceState
    {
        public GlobalDiceState(OwnedDiceId ownedDiceId, DiceRuntimeSeed battleSeed)
        {
            if (!ownedDiceId.IsValid)
            {
                throw new ArgumentException("A valid owned dice ID is required.", nameof(ownedDiceId));
            }

            if (battleSeed == null)
            {
                throw new ArgumentNullException(nameof(battleSeed));
            }

            if (!battleSeed.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(battleSeed));
            }

            OwnedDiceId = ownedDiceId;
            BattleSeed = battleSeed;
        }

        public OwnedDiceId OwnedDiceId { get; }
        public DiceRuntimeSeed BattleSeed { get; }
        public DiceId DefinitionId => BattleSeed.DefinitionId;
    }
}
