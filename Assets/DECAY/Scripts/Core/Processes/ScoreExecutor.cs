using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Owns deterministic logical SCORE execution in SlotPair order 1..6. Both sides of a pair are resolved from
    /// authoritative state before either contribution is committed; Enemy-then-Player Fact order is only a tie-break.
    /// </summary>
    public sealed class ScoreExecutor
    {
        private readonly BattleState _battleState;
        private readonly ScoreState _scoreState;
        private readonly BattleHistory _history;
        private readonly ScoreResolver _resolver;
        private readonly ScoreCompletionGate _completionGate;

        public ScoreExecutor(
            BattleState battleState,
            BoardState boardState,
            BattleInventoryState battleInventoryState,
            ScoreState scoreState,
            BattleHistory history)
        {
            _battleState = battleState ?? throw new ArgumentNullException(nameof(battleState));
            _scoreState = scoreState ?? throw new ArgumentNullException(nameof(scoreState));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _resolver = new ScoreResolver(
                boardState ?? throw new ArgumentNullException(nameof(boardState)),
                battleInventoryState ?? throw new ArgumentNullException(nameof(battleInventoryState)));
            _completionGate = new ScoreCompletionGate(_battleState, boardState, battleInventoryState, _scoreState);
        }

        internal ScoreExecutionResult ExecuteScore()
        {
            if (_battleState.CurrentPhase != BattlePhase.ScoreProcess)
                throw new InvalidOperationException($"SCORE requires phase {BattlePhase.ScoreProcess}; current phase is {_battleState.CurrentPhase}.");
            if (_scoreState.GetRoundScore(Side.Enemy) != 0 || _scoreState.GetRoundScore(Side.Player) != 0)
                throw new InvalidOperationException("A new SCORE process must begin with zero unresolved round score.");

            // Validate all current participants and current base-score arithmetic before first mutation, but
            // calculate each pair again only when it reaches its 1..6 process turn. That preserves a durable seam
            // for future earlier-slot score effects to alter later authoritative values without precomputing the
            // entire score process from stale data.
            int validatedEnemyTotal = 0;
            int validatedPlayerTotal = 0;
            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ScorePairResolution validationPair = _resolver.ResolvePair(new SlotPairId(number));
                if (validationPair.Enemy.HasValue)
                    validatedEnemyTotal = checked(validatedEnemyTotal + validationPair.Enemy.Value.ScoreContribution);
                if (validationPair.Player.HasValue)
                    validatedPlayerTotal = checked(validatedPlayerTotal + validationPair.Player.Value.ScoreContribution);
            }

            int startEnemy = _scoreState.GetRoundScore(Side.Enemy);
            int startPlayer = _scoreState.GetRoundScore(Side.Player);
            int firstFactIndex = _history.Count;
            var pairs = new List<ScorePairResolution>(BattleRules.SlotsPerSide);

            for (int number = BattleRules.FirstSlotNumber; number <= BattleRules.LastSlotNumber; number++)
            {
                ScorePairResolution pair = _resolver.ResolvePair(new SlotPairId(number));
                pairs.Add(pair);
                // Enemy then Player is only the deterministic Fact tie-break inside one same-number pair.
                ApplyIfPresent(pair.Enemy);
                ApplyIfPresent(pair.Player);
            }

            var facts = new List<BattleFact>(_history.Count - firstFactIndex);
            for (int i = firstFactIndex; i < _history.Count; i++) facts.Add(_history.Facts[i]);
            return new ScoreExecutionResult(
                _battleState.CurrentFactContext,
                pairs.AsReadOnly(),
                startEnemy,
                startPlayer,
                _scoreState.GetRoundScore(Side.Enemy),
                _scoreState.GetRoundScore(Side.Player),
                facts.AsReadOnly());
        }

        internal BattleFlowDenialReason EvaluateCompletion(ScoreExecutionResult executionResult)
        {
            return _completionGate.Evaluate(executionResult);
        }

        private void ApplyIfPresent(ScoreResolution? resolution)
        {
            if (!resolution.HasValue) return;
            _history.Record(new ApplyScoreCommand(_battleState, _scoreState, resolution.Value).Execute());
        }
    }
}
