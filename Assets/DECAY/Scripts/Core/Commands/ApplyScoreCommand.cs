using System;

namespace Decay
{
    public sealed class ApplyScoreCommand
    {
        private readonly BattleState _battleState;
        private readonly ScoreState _scoreState;
        private readonly ScoreResolution _resolution;
        private bool _wasExecuted;

        internal ApplyScoreCommand(BattleState battleState, ScoreState scoreState, ScoreResolution resolution)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _resolution = resolution;
        }

        internal ScoreAppliedFact Execute()
        {
            if (_wasExecuted) throw new InvalidOperationException("An ApplyScoreCommand can only execute once.");
            if (_battleState.CurrentPhase != BattlePhase.ScoreProcess)
                throw new InvalidOperationException("Score may only be applied during ScoreProcess.");

            ScoreContributionCompletion completion = _scoreState.ApplyContribution(_resolution.Side, _resolution.ScoreContribution);
            _wasExecuted = true;
            return new ScoreAppliedFact(
                _battleState.CurrentFactContext,
                _resolution.DiceId,
                _resolution.SlotId,
                _resolution.Side,
                _resolution.FaceIndex,
                _resolution.RollValue,
                _resolution.GeneralScoreValue,
                _resolution.FaceScoreValue,
                _resolution.ScoreContribution,
                completion.RoundScore,
                completion.TotalScore);
        }
    }
}
