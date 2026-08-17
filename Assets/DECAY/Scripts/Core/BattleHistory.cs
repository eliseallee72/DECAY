
using System;
using System.Collections.Generic;

namespace Decay
{
    public sealed class BattleHistory
    {
        private readonly List<BattleFact> _facts = new List<BattleFact>();
        private long _nextSequenceNumber = 1;

        public IReadOnlyList<BattleFact> Facts => _facts;
        public int Count => _facts.Count;

        public T Record<T>(T fact) where T : BattleFact
        {
            if (fact == null)
            {
                throw new ArgumentNullException(nameof(fact));
            }

            if (fact.HasSequenceNumber)
            {
                throw new InvalidOperationException("The supplied BattleFact has already been recorded.");
            }

            fact.AssignSequenceNumber(_nextSequenceNumber++);
            _facts.Add(fact);
            return fact;
        }
    }
}
