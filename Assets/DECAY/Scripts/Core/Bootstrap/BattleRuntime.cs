using System;

namespace Decay
{
    /// <summary>
    /// The assembled runtime objects for one battle. This object is a composition result, not a
    /// second owner of gameplay facts; each contained State remains authoritative for its domain.
    /// </summary>
    public sealed class BattleRuntime
    {
        internal BattleRuntime(
            BattleConfig config,
            GlobalInventoryState globalInventory,
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            BattleHistory history,
            MoveDiceController moveDiceController,
            BattleController battleController)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            GlobalInventory = globalInventory ?? throw new ArgumentNullException(nameof(globalInventory));
            BattleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            BoardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            BattleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));
            History = history ?? throw new ArgumentNullException(nameof(history));
            MoveDiceController = moveDiceController ?? throw new ArgumentNullException(nameof(moveDiceController));
            BattleController = battleController ?? throw new ArgumentNullException(nameof(battleController));
        }

        public BattleConfig Config { get; }
        public GlobalInventoryState GlobalInventory { get; }
        public BattleState BattleState { get; }
        public BoardState BoardState { get; }
        public BattleInventoryState BattleInventoryState { get; }
        public BattleHistory History { get; }
        public MoveDiceController MoveDiceController { get; }
        public BattleController BattleController { get; }
    }
}
