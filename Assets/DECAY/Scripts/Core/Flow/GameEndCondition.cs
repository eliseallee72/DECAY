using System;

namespace Decay
{
    /// <summary>
    /// Evaluates whether the current game must end after the Score Process has reached RoundEnd.
    /// Structural phase edges remain the responsibility of BattlePhaseTransitionValidator.
    /// </summary>
    public sealed class GameEndCondition
    {
        private readonly BattleState _battleState;
        private readonly BoardState _boardState;

        public GameEndCondition(BattleState battleState, BoardState boardState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _boardState = boardState ?? throw new ArgumentNullException(nameof(boardState));
        }

        public bool IsRoundLimitReached => _battleState.CurrentRoundNumber >= _battleState.RoundsPerGame;
        public bool IsPlayerSideFullyBroken => _boardState.AreAllSlotsBroken(Side.Player);
        public bool IsEnemySideFullyBroken => _boardState.AreAllSlotsBroken(Side.Enemy);
        public bool IsGameEndRequired => IsRoundLimitReached || IsPlayerSideFullyBroken || IsEnemySideFullyBroken;
        public bool IsBoardBreakEndRequired => IsPlayerSideFullyBroken || IsEnemySideFullyBroken;
    }
}
