namespace Decay
{
    /// <summary>
    /// Answers whether the current battle phase permits this side and source/target category to move.
    /// EnemySetup and PlayerSetup allow board/inventory movement for their named Side. Reposition is
    /// board-only for its named Side.
    /// </summary>
    internal sealed class MovementPhaseGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            switch (context.BattleState.CurrentPhase)
            {
                case BattlePhase.EnemySetup:
                    return EvaluateSetup(context, Side.Enemy);

                case BattlePhase.PlayerSetup:
                    return EvaluateSetup(context, Side.Player);

                case BattlePhase.EnemyReposition:
                    return EvaluateReposition(context, Side.Enemy);

                case BattlePhase.PlayerReposition:
                    return EvaluateReposition(context, Side.Player);

                default:
                    return MoveDiceDenialReason.PhaseDoesNotAllowMovement;
            }
        }

        private static MoveDiceDenialReason EvaluateSetup(MoveDiceGateContext context, Side permittedSide)
        {
            return context.Request.ActingSide == permittedSide
                ? MoveDiceDenialReason.None
                : MoveDiceDenialReason.ActingSideDoesNotMatchSetupTurn;
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
