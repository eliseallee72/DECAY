using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Constructs one battle from permanent Player inventory data plus an authored Enemy roster.
    /// It creates battle-local dice copies and wires the existing authoritative rule systems together.
    /// </summary>
    public sealed class BattleBootstrapper
    {
        public BattleRuntime Create(
            BattleConfig config,
            GlobalInventoryState globalInventory,
            IEnumerable<OwnedDiceId> selectedPlayerDiceIds,
            IEnumerable<DiceRuntimeSeed> enemyDiceSeeds,
            IRandomSource primaryRandomSource,
            IRandomSource fallbackRandomSource)
        {
            RequireValidConfig(config);
            if (globalInventory == null)
            {
                throw new ArgumentNullException(nameof(globalInventory));
            }

            if (selectedPlayerDiceIds == null)
            {
                throw new ArgumentNullException(nameof(selectedPlayerDiceIds));
            }

            if (enemyDiceSeeds == null)
            {
                throw new ArgumentNullException(nameof(enemyDiceSeeds));
            }

            if (primaryRandomSource == null)
            {
                throw new ArgumentNullException(nameof(primaryRandomSource));
            }

            if (fallbackRandomSource == null)
            {
                throw new ArgumentNullException(nameof(fallbackRandomSource));
            }

            List<OwnedDiceId> playerIds = CopyAndValidatePlayerSelection(
                selectedPlayerDiceIds,
                globalInventory,
                config.BattleInventoryCapacity);
            List<DiceRuntimeSeed> enemySeeds = CopyAndValidateEnemyRoster(
                enemyDiceSeeds,
                config.BattleInventoryCapacity);

            var runtimeDice = new List<DiceRuntimeState>(playerIds.Count + enemySeeds.Count);
            long nextInstanceId = 1;

            for (int i = 0; i < playerIds.Count; i++)
            {
                OwnedDiceId ownedDiceId = playerIds[i];
                GlobalDiceState globalDice = globalInventory.GetDice(ownedDiceId);
                runtimeDice.Add(DiceRuntimeState.CreatePlayerDice(
                    new DiceInstanceId(nextInstanceId++),
                    ownedDiceId,
                    globalDice.BattleSeed));
            }

            for (int i = 0; i < enemySeeds.Count; i++)
            {
                runtimeDice.Add(DiceRuntimeState.CreateEnemyDice(
                    new DiceInstanceId(nextInstanceId++),
                    enemySeeds[i]));
            }

            var battleState = new BattleState(config);
            var boardState = new BoardState();
            var battleInventoryState = new BattleInventoryState(config, runtimeDice);
            var history = new BattleHistory();
            var phaseController = new BattlePhaseController(
                battleState,
                boardState,
                new BattlePhaseTransitionValidator(),
                history);
            var moveDiceController = new MoveDiceController(
                battleState,
                boardState,
                battleInventoryState,
                history);
            var rollExecutor = new RollExecutor(
                battleState,
                boardState,
                battleInventoryState,
                history,
                primaryRandomSource,
                fallbackRandomSource);
            var decayExecutor = new DecayExecutor(
                battleState,
                boardState,
                battleInventoryState,
                history);
            var battleController = new BattleController(
                battleState,
                phaseController,
                history,
                rollExecutor,
                decayExecutor);

            return new BattleRuntime(
                config,
                globalInventory,
                battleState,
                boardState,
                battleInventoryState,
                history,
                moveDiceController,
                battleController);
        }

        private static void RequireValidConfig(BattleConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (!config.TryValidate(out string error))
            {
                throw new ArgumentException(error, nameof(config));
            }
        }

        private static List<OwnedDiceId> CopyAndValidatePlayerSelection(
            IEnumerable<OwnedDiceId> selectedPlayerDiceIds,
            GlobalInventoryState globalInventory,
            int capacity)
        {
            var result = new List<OwnedDiceId>();
            var uniqueIds = new HashSet<OwnedDiceId>();

            foreach (OwnedDiceId ownedDiceId in selectedPlayerDiceIds)
            {
                if (!ownedDiceId.IsValid)
                {
                    throw new ArgumentException("Player battle roster contains an invalid owned dice ID.", nameof(selectedPlayerDiceIds));
                }

                if (!uniqueIds.Add(ownedDiceId))
                {
                    throw new ArgumentException($"Owned dice ID {ownedDiceId} appears more than once in the Player battle roster.", nameof(selectedPlayerDiceIds));
                }

                if (!globalInventory.Contains(ownedDiceId))
                {
                    throw new ArgumentException($"Global Inventory does not contain selected owned dice {ownedDiceId}.", nameof(selectedPlayerDiceIds));
                }

                result.Add(ownedDiceId);
                if (result.Count > capacity)
                {
                    throw new ArgumentException($"Player battle roster exceeds configured capacity {capacity}.", nameof(selectedPlayerDiceIds));
                }
            }

            return result;
        }

        private static List<DiceRuntimeSeed> CopyAndValidateEnemyRoster(
            IEnumerable<DiceRuntimeSeed> enemyDiceSeeds,
            int capacity)
        {
            var result = new List<DiceRuntimeSeed>();
            foreach (DiceRuntimeSeed seed in enemyDiceSeeds)
            {
                if (seed == null)
                {
                    throw new ArgumentException("Enemy battle roster cannot contain a null dice seed.", nameof(enemyDiceSeeds));
                }

                if (!seed.TryValidate(out string error))
                {
                    throw new ArgumentException($"Enemy battle roster contains invalid dice data: {error}", nameof(enemyDiceSeeds));
                }

                result.Add(seed);
                if (result.Count > capacity)
                {
                    throw new ArgumentException($"Enemy battle roster exceeds configured capacity {capacity}.", nameof(enemyDiceSeeds));
                }
            }

            return result;
        }
    }
}
