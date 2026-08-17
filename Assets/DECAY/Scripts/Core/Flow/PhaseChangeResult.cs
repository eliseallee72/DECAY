namespace Decay
{
    public sealed class PhaseChangeResult
    {
        private PhaseChangeResult(bool isApproved, PhaseChangeDenialReason denialReason, PhaseChangedFact fact)
        {
            IsApproved = isApproved;
            DenialReason = denialReason;
            Fact = fact;
        }

        public bool IsApproved { get; }
        public PhaseChangeDenialReason DenialReason { get; }
        public PhaseChangedFact Fact { get; }

        internal static PhaseChangeResult Approved(PhaseChangedFact fact)
        {
            return new PhaseChangeResult(true, PhaseChangeDenialReason.None, fact);
        }

        internal static PhaseChangeResult Rejected(PhaseChangeDenialReason denialReason)
        {
            return new PhaseChangeResult(false, denialReason, null);
        }
    }
}
