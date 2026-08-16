namespace Decay
{
    /// <summary>
    /// Identifies which side currently owns interactive placement and movement during BattlePhase.Setup.
    /// Enemy Setup always precedes Player Setup. This refines Setup without creating competing battle phases.
    /// </summary>
    public enum BattleSetupTurn
    {
        Enemy = 0,
        Player = 1
    }
}
