using System;
using System.Collections.Generic;

namespace Decay
{
    internal sealed class GameEndExecutionResult
    {
        private readonly IReadOnlyList<BattleFact> _facts;

        internal GameEndExecutionResult(
            BattleFactContext context,
            GameScoreCompletion scoreCompletion,
            bool preparedNextGame,
            IReadOnlyList<BattleFact> facts)
        {
            if (context.Phase != BattlePhase.GameEnd)
                throw new ArgumentException("GameEnd result must belong to GameEnd.", nameof(context));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            Context = context;
            ScoreCompletion = scoreCompletion;
            PreparedNextGame = preparedNextGame;
            _facts = new List<BattleFact>(facts).AsReadOnly();
        }

        internal BattleFactContext Context { get; }
        internal GameScoreCompletion ScoreCompletion { get; }
        internal bool PreparedNextGame { get; }
        internal IReadOnlyList<BattleFact> Facts => _facts;
    }
}
