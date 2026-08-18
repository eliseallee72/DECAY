using System;

namespace Decay
{
    internal sealed class FinalizeRoundScoreCommand
    {
        private readonly BattleState _battleState;
        private readonly ScoreState _scoreState;
        private bool _wasExecuted;

        internal FinalizeRoundScoreCommand(BattleState battleState, ScoreState scoreState)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
        }

        internal RoundScoreCompletion Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("A FinalizeRoundScoreCommand can only execute once.");
            if (_battleState.CurrentPhase != BattlePhase.RoundEnd)
                throw new InvalidOperationException("Round score may only be finalized during RoundEnd.");
            _wasExecuted = true;
            return _scoreState.FinalizeRound();
        }
    }
}
