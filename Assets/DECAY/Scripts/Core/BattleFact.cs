
using System;

namespace Decay
{
    public abstract class BattleFact
    {
        public long SequenceNumber { get; private set; }
        public bool HasSequenceNumber => SequenceNumber > 0;

        internal void AssignSequenceNumber(long sequenceNumber)
        {
            if (sequenceNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sequenceNumber), sequenceNumber, "Fact sequence number must be positive.");
            }

            if (HasSequenceNumber)
            {
                throw new InvalidOperationException("A BattleFact can only be recorded once.");
            }

            SequenceNumber = sequenceNumber;
        }
    }
}
