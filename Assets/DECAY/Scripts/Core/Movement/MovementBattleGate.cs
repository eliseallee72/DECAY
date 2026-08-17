namespace Decay
{
    /// <summary>
    /// Answers whether the current battle state permits normal movement requests at all.
    /// </summary>
    internal sealed class MovementBattleGate : IMoveDiceGate
    {
        public MoveDiceDenialReason Evaluate(MoveDiceGateContext context)
        {
            return context.BattleState.IsBattleComplete
                ? MoveDiceDenialReason.BattleAlreadyComplete
                : MoveDiceDenialReason.None;
        }
    }
}
