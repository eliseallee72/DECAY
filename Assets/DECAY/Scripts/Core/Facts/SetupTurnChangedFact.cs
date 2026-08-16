namespace Decay
{
    /// <summary>
    /// Records the authoritative handoff from Enemy Setup to Player Setup inside one Setup phase.
    /// </summary>
    public sealed class SetupTurnChangedFact : BattleFact
    {
        internal SetupTurnChangedFact(
            BattleFactContext context,
            BattleSetupTurn previousTurn,
            BattleSetupTurn currentTurn)
        {
            Context = context;
            PreviousTurn = previousTurn;
            CurrentTurn = currentTurn;
        }

        public BattleFactContext Context { get; }
        public BattleSetupTurn PreviousTurn { get; }
        public BattleSetupTurn CurrentTurn { get; }
        public int GameNumber => Context.GameNumber;
        public int RoundNumber => Context.RoundNumber;
    }
}
