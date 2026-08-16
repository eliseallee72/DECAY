namespace Decay
{
    /// <summary>
    /// Answers whether the acting side controls the requested tracked dice.
    /// </summary>
    internal sealed class MovementControlGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            if (!context.IsTracked || context.DiceState == null)
            {
                return MoveDiceDenialReason.DiceNotTracked;
            }

            return context.DiceState.Owner == context.Request.ActingSide
                ? MoveDiceDenialReason.None
                : MoveDiceDenialReason.DiceOwnedByOtherSide;
        }
    }
}
