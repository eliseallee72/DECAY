using System;

namespace Decay
{
    public sealed class MoveDiceResult
    {
        private MoveDiceResult(bool isApproved, MoveDiceDenialReason denialReason, BattleFact fact)
        {
            IsApproved = isApproved;
            DenialReason = denialReason;
            Fact = fact;
        }

        public bool IsApproved { get; }
        public bool IsRejected => !IsApproved;
        public MoveDiceDenialReason DenialReason { get; }
        public BattleFact Fact { get; }

        internal static MoveDiceResult Approved(BattleFact fact)
        {
            return new MoveDiceResult(
                true,
                MoveDiceDenialReason.None,
                fact ?? throw new ArgumentNullException(nameof(fact)));
        }

        internal static MoveDiceResult Rejected(MoveDiceDenialReason denialReason)
        {
            if (denialReason == MoveDiceDenialReason.None)
            {
                throw new ArgumentException("A rejected movement result requires a denial reason.", nameof(denialReason));
            }

            return new MoveDiceResult(false, denialReason, null);
        }
    }
}
