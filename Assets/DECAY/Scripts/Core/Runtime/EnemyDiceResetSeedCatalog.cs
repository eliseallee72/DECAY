using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable battle-start reset source for Enemy dice. Player reset authority remains GlobalInventoryState.
    /// This exists only because Enemy dice have no permanent Global Inventory in the current DECAY model.
    /// </summary>
    internal sealed class EnemyDiceResetSeedCatalog
    {
        private readonly Dictionary<DiceInstanceId, DiceRuntimeSeed> _seedsByDiceId;

        internal EnemyDiceResetSeedCatalog(IEnumerable<KeyValuePair<DiceInstanceId, DiceRuntimeSeed>> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            _seedsByDiceId = new Dictionary<DiceInstanceId, DiceRuntimeSeed>();
            foreach (KeyValuePair<DiceInstanceId, DiceRuntimeSeed> entry in entries)
            {
                if (!entry.Key.IsValid) throw new ArgumentException("Enemy reset seed requires a valid dice instance ID.", nameof(entries));
                if (entry.Value == null)
                    throw new ArgumentException($"Enemy reset seed for {entry.Key} is missing.", nameof(entries));
                if (!entry.Value.TryValidate(out string error))
                    throw new ArgumentException($"Enemy reset seed for {entry.Key} is invalid: {error}", nameof(entries));
                if (!_seedsByDiceId.TryAdd(entry.Key, entry.Value))
                    throw new ArgumentException($"Enemy reset seed repeats dice {entry.Key}.", nameof(entries));
            }
        }

        internal bool TryGet(DiceInstanceId diceId, out DiceRuntimeSeed seed)
        {
            if (!diceId.IsValid)
            {
                seed = null;
                return false;
            }
            return _seedsByDiceId.TryGetValue(diceId, out seed);
        }

        internal DiceRuntimeSeed GetRequired(DiceInstanceId diceId)
        {
            if (!TryGet(diceId, out DiceRuntimeSeed seed))
                throw new InvalidOperationException($"No battle-start Enemy reset seed exists for dice {diceId}.");
            return seed;
        }
    }
}
