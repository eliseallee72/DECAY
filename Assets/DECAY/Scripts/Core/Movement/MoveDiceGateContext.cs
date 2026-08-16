using System;

namespace Decay
{
    /// <summary>
    /// Resolved read-only evaluation context for one synchronous movement request.
    /// It owns no gameplay state and performs no mutation. The constructor is internal so only the movement
    /// authority creates contexts, while future process/tutorial Gates may read them from other assemblies.
    /// </summary>
    public sealed class MoveDiceGateContext
    {
        internal MoveDiceGateContext(
            MoveDiceRequest request,
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            BattleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            BoardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
            BattleInventoryState = battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState));

            IsTracked = BattleInventoryState.TryGetDice(Request.DiceId, out DiceRuntimeState diceState);
            DiceState = diceState;
            IsInInventory = IsTracked && BattleInventoryState.IsInInventory(Request.DiceId);
            IsOnBoard = BoardState.TryGetSlotOfDice(Request.DiceId, out SlotId sourceSlot);
            SourceSlot = sourceSlot;

            if (Request.Target.Kind == MoveDiceTargetKind.BoardSlot)
            {
                DestinationSlot = BoardState.GetSlot(Request.Target.BoardSlot);
            }
        }

        public MoveDiceRequest Request { get; }
        public BattleState BattleState { get; }
        public BoardState BoardState { get; }
        public BattleInventoryState BattleInventoryState { get; }
        public bool IsTracked { get; }
        public DiceRuntimeState DiceState { get; }
        public bool IsInInventory { get; }
        public bool IsOnBoard { get; }
        public SlotId SourceSlot { get; }
        public SlotState DestinationSlot { get; }
    }
}
