using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Baseline deterministic Enemy setup strategy. It fills empty Unbroken Enemy slots from left to right
    /// using the Enemy Battle Inventory's stable order. The strategy is intentionally simple, but it is a
    /// real replaceable planner rather than a presentation shortcut or direct BoardState mutation.
    /// </summary>
    public sealed class FillAvailableEnemySlotsPlanner : IEnemySetupPlanner
    {
        public IReadOnlyList<MoveDiceRequest> CreatePlan(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState)
        {
            if (battleState == null) throw new ArgumentNullException(nameof(battleState));
            if (boardState == null) throw new ArgumentNullException(nameof(boardState));
            if (battleInventoryState == null) throw new ArgumentNullException(nameof(battleInventoryState));

            if (battleState.CurrentPhase != BattlePhase.Setup)
            {
                throw new InvalidOperationException(
                    $"Enemy setup planning requires phase {BattlePhase.Setup}; current phase is {battleState.CurrentPhase}.");
            }

            IReadOnlyList<DiceInstanceId> enemyInventory = battleInventoryState.InventoryDiceIds(Side.Enemy);
            var destinations = new List<SlotId>(BattleRules.SlotsPerSide);
            for (int slotNumber = BattleRules.FirstSlotNumber; slotNumber <= BattleRules.LastSlotNumber; slotNumber++)
            {
                var slotId = new SlotId(Side.Enemy, slotNumber);
                SlotState slot = boardState.GetSlot(slotId);
                if (slot.Condition == SlotCondition.Unbroken && !slot.HasDice)
                {
                    destinations.Add(slotId);
                }
            }

            int movementCount = Math.Min(enemyInventory.Count, destinations.Count);
            var requests = new List<MoveDiceRequest>(movementCount);
            for (int i = 0; i < movementCount; i++)
            {
                requests.Add(new MoveDiceRequest(
                    Side.Enemy,
                    enemyInventory[i],
                    MoveDiceTarget.Board(destinations[i])));
            }

            return requests.AsReadOnly();
        }
    }
}
