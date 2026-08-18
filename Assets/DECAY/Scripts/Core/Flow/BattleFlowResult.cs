using System;
using System.Collections.Generic;

namespace Decay
{
    /// <summary>
    /// Result of one bounded BattleController flow request. Approved facts are the authoritative
    /// changes produced by that request, in the same order they were recorded in BattleHistory.
    /// </summary>
    public sealed class BattleFlowResult
    {
        private BattleFlowResult(
            bool isApproved,
            BattleFlowDenialReason denialReason,
            IReadOnlyList<BattleFact> facts)
        {
            IsApproved = isApproved;
            DenialReason = denialReason;
            Facts = facts ?? throw new ArgumentNullException(nameof(facts));
        }

        public bool IsApproved { get; }
        public bool IsRejected => !IsApproved;
        public BattleFlowDenialReason DenialReason { get; }
        public IReadOnlyList<BattleFact> Facts { get; }

        internal static BattleFlowResult Approved(IReadOnlyList<BattleFact> facts)
        {
            return new BattleFlowResult(true, BattleFlowDenialReason.None, facts);
        }

        internal static BattleFlowResult Rejected(BattleFlowDenialReason denialReason)
        {
            if (denialReason == BattleFlowDenialReason.None)
            {
                throw new ArgumentException("A rejected battle flow result requires a denial reason.", nameof(denialReason));
            }

            return new BattleFlowResult(false, denialReason, Array.Empty<BattleFact>());
        }
    }
}
