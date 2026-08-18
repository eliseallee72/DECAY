using System;

namespace Decay
{
    internal sealed class FinalizeGameScoreCommand
    {
        private readonly BattleState _battleState;
        private readonly ScoreState _scoreState;
        private bool _wasExecuted;

        internal FinalizeGameScoreCommand(BattleState battleState, ScoreState scoreState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
        }

        internal GameScoreCompletion Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("A FinalizeGameScoreCommand can only execute once.");
            if (_battleState.CurrentPhase != BattlePhase.GameEnd)
                throw new InvalidOperationException("Game score may only be finalized during GameEnd.");
            _wasExecuted = true;
            return _scoreState.FinalizeGame();
        }
    }
}
