using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Decay
{
    public sealed class BattleInventoryState
    {
        private readonly Dictionary<DiceInstanceId, DiceRuntimeState> _diceById = new Dictionary<DiceInstanceId, DiceRuntimeState>();
        private readonly HashSet<OwnedDiceId> _playerSourceOwnedDiceIds = new HashSet<OwnedDiceId>();
        private readonly List<DiceInstanceId> _enemyInventoryDiceIds = new List<DiceInstanceId>();
        private readonly List<DiceInstanceId> _playerInventoryDiceIds = new List<DiceInstanceId>();
        private readonly ReadOnlyCollection<DiceInstanceId> _enemyInventoryDiceIdsView;
        private readonly ReadOnlyCollection<DiceInstanceId> _playerInventoryDiceIdsView;
        private int _enemyTrackedCount;
        private int _playerTrackedCount;

        public BattleInventoryState(BattleConfig config, IEnumerable<DiceRuntimeState> dice)
            : this(RequireCapacity(config), dice)
        {
        }

        public BattleInventoryState(int capacityPerSide, IEnumerable<DiceRuntimeState> dice)
        {
            if (capacityPerSide < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacityPerSide), capacityPerSide, "Battle inventory capacity must be at least 1.");
            }

            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            CapacityPerSide = capacityPerSide;
            _enemyInventoryDiceIdsView = _enemyInventoryDiceIds.AsReadOnly();
            _playerInventoryDiceIdsView = _playerInventoryDiceIds.AsReadOnly();

            foreach (DiceRuntimeState diceState in dice)
            {
                AddInitialDice(diceState);
            }
        }

        public int CapacityPerSide { get; }
        public int TotalTrackedCount => _diceById.Count;

        public int TrackedCount(Side side)
        {
            RequireValidSide(side);
            return side == Side.Enemy ? _enemyTrackedCount : _playerTrackedCount;
        }

        public int InventoryCount(Side side)
        {
            return GetInventoryList(side).Count;
        }

        /// <summary>
        /// Current membership view. Its present list order is not gameplay authority and may be
        /// presented differently by the future drawer/carousel.
        /// </summary>
        public IReadOnlyList<DiceInstanceId> InventoryDiceIds(Side side)
        {
            RequireValidSide(side);
            return side == Side.Enemy ? _enemyInventoryDiceIdsView : _playerInventoryDiceIdsView;
        }

        public bool ContainsDice(DiceInstanceId diceId)
        {
            return diceId.IsValid && _diceById.ContainsKey(diceId);
        }

        public bool IsInInventory(DiceInstanceId diceId)
        {
            if (!TryGetDice(diceId, out DiceRuntimeState diceState))
            {
                return false;
            }

            return GetInventoryList(diceState.Owner).Contains(diceId);
        }

        public bool TryGetDice(DiceInstanceId diceId, out DiceRuntimeState diceState)
        {
            if (!diceId.IsValid)
            {
                diceState = null;
                return false;
            }

            return _diceById.TryGetValue(diceId, out diceState);
        }

        public DiceRuntimeState GetDice(DiceInstanceId diceId)
        {
            if (!TryGetDice(diceId, out DiceRuntimeState diceState))
            {
                throw new KeyNotFoundException($"Battle Inventory does not track dice {diceId}.");
            }

            return diceState;
        }

        internal DiceRuntimeState RemoveFromInventory(DiceInstanceId diceId)
        {
            DiceRuntimeState diceState = RequireCanRemoveFromInventory(diceId);
            GetInventoryList(diceState.Owner).Remove(diceId);
            return diceState;
        }

        internal void ReturnToInventory(DiceInstanceId diceId)
        {
            DiceRuntimeState diceState = RequireCanReturnToInventory(diceId);
            GetInventoryList(diceState.Owner).Add(diceId);
        }

        internal DiceRuntimeState RequireCanRemoveFromInventory(DiceInstanceId diceId)
        {
            DiceRuntimeState diceState = GetDice(diceId);
            if (!GetInventoryList(diceState.Owner).Contains(diceId))
            {
                throw new InvalidOperationException($"Dice {diceId} is not currently in the {diceState.Owner} Battle Inventory.");
            }

            return diceState;
        }

        internal DiceRuntimeState RequireCanReturnToInventory(DiceInstanceId diceId)
        {
            DiceRuntimeState diceState = GetDice(diceId);
            List<DiceInstanceId> inventory = GetInventoryList(diceState.Owner);

            if (diceState.IsDecayedForCurrentGame)
            {
                throw new InvalidOperationException(
                    $"DECAYED dice {diceId} cannot be a current Battle Inventory member until a new-game repopulation resets it.");
            }

            if (inventory.Contains(diceId))
            {
                throw new InvalidOperationException($"Dice {diceId} is already in the {diceState.Owner} Battle Inventory.");
            }

            return diceState;
        }

        private void AddInitialDice(DiceRuntimeState diceState)
        {
            if (diceState == null)
            {
                throw new ArgumentException("Battle Inventory cannot contain a null dice state.");
            }

            if (diceState.IsDecayedForCurrentGame)
            {
                throw new ArgumentException(
                    $"DECAYED dice {diceState.InstanceId} cannot begin as a current Battle Inventory member.");
            }

            if (_diceById.ContainsKey(diceState.InstanceId))
            {
                throw new ArgumentException($"Dice instance ID {diceState.InstanceId} appears more than once in Battle Inventory.");
            }

            if (diceState.Owner == Side.Player)
            {
                if (!diceState.HasSourceOwnedDice || !diceState.SourceOwnedDiceId.IsValid)
                {
                    throw new ArgumentException(
                        $"Player dice {diceState.InstanceId} must identify the permanent owned dice that supplied its battle state.");
                }

                if (!_playerSourceOwnedDiceIds.Add(diceState.SourceOwnedDiceId))
                {
                    throw new ArgumentException(
                        $"Owned dice ID {diceState.SourceOwnedDiceId} appears more than once in the Player battle roster.");
                }
            }

            int sideCount = diceState.Owner == Side.Enemy ? _enemyTrackedCount : _playerTrackedCount;
            if (sideCount >= CapacityPerSide)
            {
                if (diceState.Owner == Side.Player)
                {
                    _playerSourceOwnedDiceIds.Remove(diceState.SourceOwnedDiceId);
                }

                throw new ArgumentException(
                    $"{diceState.Owner} Battle Inventory exceeds its capacity of {CapacityPerSide} dice.");
            }

            _diceById.Add(diceState.InstanceId, diceState);
            GetInventoryList(diceState.Owner).Add(diceState.InstanceId);

            if (diceState.Owner == Side.Enemy)
            {
                _enemyTrackedCount++;
            }
            else
            {
                _playerTrackedCount++;
            }
        }

        private List<DiceInstanceId> GetInventoryList(Side side)
        {
            RequireValidSide(side);
            return side == Side.Enemy ? _enemyInventoryDiceIds : _playerInventoryDiceIds;
        }

        private static int RequireCapacity(BattleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(config));
            }

            return config.BattleInventoryCapacity;
        }

        private static void RequireValidSide(Side side)
        {
            if (!Enum.IsDefined(typeof(Side), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Side must be Enemy or Player.");
            }
        }
    }
}
