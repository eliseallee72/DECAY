using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Decay
{
    public sealed class GlobalInventoryState
    {
        private readonly Dictionary<OwnedDiceId, GlobalDiceState> _diceByOwnedId = new Dictionary<OwnedDiceId, GlobalDiceState>();
        private readonly List<OwnedDiceId> _ownedDiceIds = new List<OwnedDiceId>();
        private readonly ReadOnlyCollection<OwnedDiceId> _ownedDiceIdsView;

        public GlobalInventoryState(IEnumerable<GlobalDiceState> dice)
        {
            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            _ownedDiceIdsView = _ownedDiceIds.AsReadOnly();

            foreach (GlobalDiceState diceState in dice)
            {
                if (diceState == null)
                {
                    throw new ArgumentException("Global inventory cannot contain a null dice state.", nameof(dice));
                }

                if (_diceByOwnedId.ContainsKey(diceState.OwnedDiceId))
                {
                    throw new ArgumentException($"Owned dice ID {diceState.OwnedDiceId} appears more than once in Global Inventory.", nameof(dice));
                }

                _diceByOwnedId.Add(diceState.OwnedDiceId, diceState);
                _ownedDiceIds.Add(diceState.OwnedDiceId);
            }
        }

        public int Count => _diceByOwnedId.Count;
        public IReadOnlyList<OwnedDiceId> OwnedDiceIds => _ownedDiceIdsView;

        public bool Contains(OwnedDiceId ownedDiceId)
        {
            return ownedDiceId.IsValid && _diceByOwnedId.ContainsKey(ownedDiceId);
        }

        public bool TryGetDice(OwnedDiceId ownedDiceId, out GlobalDiceState diceState)
        {
            if (!ownedDiceId.IsValid)
            {
                diceState = null;
                return false;
            }

            return _diceByOwnedId.TryGetValue(ownedDiceId, out diceState);
        }

        public GlobalDiceState GetDice(OwnedDiceId ownedDiceId)
        {
            if (!TryGetDice(ownedDiceId, out GlobalDiceState diceState))
            {
                throw new KeyNotFoundException($"Global Inventory does not contain owned dice {ownedDiceId}.");
            }

            return diceState;
        }
    }
}
