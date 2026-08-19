using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Immutable proof of one committed logical SCORE pass. ScoreState remains score authority.
    /// </summary>
    internal sealed class ScoreExecutionResult
    {
        private readonly IReadOnlyList<ScorePairResolution> _pairs;
        private readonly IReadOnlyList<BattleFact> _facts;

        internal ScoreExecutionResult(
            BattleFactContext context,
            IReadOnlyList<ScorePairResolution> pairs,
            int startingEnemyRoundScore,
            int startingPlayerRoundScore,
            int endingEnemyRoundScore,
            int endingPlayerRoundScore,
            IReadOnlyList<BattleFact> facts)
        {
            if (context.Phase != BattlePhase.ScoreProcess)
                throw new ArgumentException("SCORE execution result must belong to ScoreProcess.", nameof(context));
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (pairs.Count != BattleRules.SlotsPerSide)
                throw new ArgumentException($"SCORE must resolve exactly {BattleRules.SlotsPerSide} slot pairs.", nameof(pairs));

            var copy = new List<ScorePairResolution>(pairs.Count);
            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].PairId.Number != BattleRules.FirstSlotNumber + i)
                    throw new ArgumentException("SCORE pair resolutions must be ordered exactly 1 through 6.", nameof(pairs));
                copy.Add(pairs[i]);
            }

            Context = context;
            _pairs = copy.AsReadOnly();
            StartingEnemyRoundScore = startingEnemyRoundScore;
            StartingPlayerRoundScore = startingPlayerRoundScore;
            EndingEnemyRoundScore = endingEnemyRoundScore;
            EndingPlayerRoundScore = endingPlayerRoundScore;
            _facts = new List<BattleFact>(facts).AsReadOnly();
        }

        internal BattleFactContext Context { get; }
        internal IReadOnlyList<ScorePairResolution> Pairs => _pairs;
        internal int StartingEnemyRoundScore { get; }
        internal int StartingPlayerRoundScore { get; }
        internal int EndingEnemyRoundScore { get; }
        internal int EndingPlayerRoundScore { get; }
        internal IReadOnlyList<BattleFact> Facts => _facts;
    }
}
