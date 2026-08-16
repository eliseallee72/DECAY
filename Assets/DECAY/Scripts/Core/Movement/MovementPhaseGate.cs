namespace Decay
{
    /// <summary>
    /// Answers whether the current battle phase permits this side and source/target category to move.
    /// Setup allows board/inventory movement for either side. Reposition is board-only for its named side.
    /// </summary>
    internal sealed class MovementPhaseGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            switch (context.BattleState.CurrentPhase)
            {
                case BattlePhase.Setup:
                    return MoveDiceDenialReason.None;

                case BattlePhase.EnemyReposition:
                    return EvaluateReposition(context, Side.Enemy);

                case BattlePhase.PlayerReposition:
                    return EvaluateReposition(context, Side.Player);

                default:
                    return MoveDiceDenialReason.PhaseDoesNotAllowMovement;
            }
        }

        private static MoveDiceDenialReason EvaluateReposition(MoveDiceGateContext context, Side permittedSide)
        {
            if (context.Request.ActingSide != permittedSide)
            {
                return MoveDiceDenialReason.ActingSideDoesNotMatchPhase;
            }

            if (!context.IsOnBoard)
            {
                return MoveDiceDenialReason.RepositionRequiresBoardSource;
            }

            if (context.Request.Target.Kind == MoveDiceTargetKind.BattleInventory)
            {
                return MoveDiceDenialReason.InventoryNotAllowedDuringReposition;
            }

            return MoveDiceDenialReason.None;
        }
    }
}
