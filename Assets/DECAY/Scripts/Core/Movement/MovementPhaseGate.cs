using System;

namespace Decay
{
    /// <summary>
    /// Answers whether the current battle phase permits this side and source/target category to move.
    /// Setup is subdivided authoritatively into Enemy Setup then Player Setup. Reposition is board-only
    /// for its named side.
    /// </summary>
    internal sealed class MovementPhaseGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            switch (context.BattleState.CurrentPhase)
            {
                case BattlePhase.Setup:
                    return EvaluateSetup(context);

                case BattlePhase.EnemyReposition:
                    return EvaluateReposition(context, Side.Enemy);

                case BattlePhase.PlayerReposition:
                    return EvaluateReposition(context, Side.Player);

                default:
                    return MoveDiceDenialReason.PhaseDoesNotAllowMovement;
            }
        }

        private static MoveDiceDenialReason EvaluateSetup(MoveDiceGateContext context)
        {
            Side permittedSide;
            switch (context.BattleState.CurrentSetupTurn)
            {
                case BattleSetupTurn.Enemy:
                    permittedSide = Side.Enemy;
                    break;

                case BattleSetupTurn.Player:
                    permittedSide = Side.Player;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported setup turn {context.BattleState.CurrentSetupTurn}.");
            }

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
